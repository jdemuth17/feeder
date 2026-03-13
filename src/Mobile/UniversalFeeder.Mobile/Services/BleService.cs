using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.Exceptions;
using Microsoft.Maui.ApplicationModel;
using System.Text;
#if ANDROID
using System.Collections.Concurrent;
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Java.Util;
#endif

namespace UniversalFeeder.Mobile.Services
{
    public class BleService
    {
        private static readonly Guid ServiceUuid = Guid.Parse("4fafc201-1fb5-459e-8fcc-c5c9c331914b");
        private static readonly Guid SsidCharUuid = Guid.Parse("beb5483e-36e1-4688-b7f5-ea07361b26a8");
        private static readonly Guid PassCharUuid = Guid.Parse("d6e98ba1-8ef4-4594-ba04-0390ea000001");
        private static readonly Guid IpCharUuid = Guid.Parse("e2a00001-8ef4-4594-ba04-0390ea000001");
        private static readonly ConnectParameters DirectConnectParameters = new(autoConnect: false, forceBleTransport: true, connectionParameterSet: ConnectionParameterSet.None);
        private const string LogTag = "UniversalFeeder.BLE";

        private readonly IAdapter _adapter;
        private readonly IBluetoothLE _bluetooth;
        private EventHandler<Plugin.BLE.Abstractions.EventArgs.DeviceEventArgs>? _discoveredHandler;

        public BleService()
        {
            _bluetooth = CrossBluetoothLE.Current;
            _adapter = CrossBluetoothLE.Current.Adapter;
            _adapter.ScanTimeout = 10000; // 10 second scan timeout

            _adapter.DeviceConnected += (sender, args) =>
            {
                LogBle($"BLE adapter event: connected id={args.Device.Id} name={GetDisplayName(args.Device)} state={args.Device.State}");
            };

            _adapter.DeviceDisconnected += (sender, args) =>
            {
                LogBle($"BLE adapter event: disconnected id={args.Device.Id} name={GetDisplayName(args.Device)} state={args.Device.State}");
            };

            _adapter.DeviceConnectionLost += (sender, args) =>
            {
                LogBle($"BLE adapter event: connection lost id={args.Device.Id} name={GetDisplayName(args.Device)} state={args.Device.State}");
            };
        }

        private static void LogBle(string message)
        {
#if ANDROID
            global::Android.Util.Log.Debug(LogTag, message);
#else
            System.Diagnostics.Debug.WriteLine(message);
#endif
        }

        public bool IsBluetoothOn => _bluetooth.IsOn;

        public string GetDisplayName(IDevice device)
        {
            var advertisedName = GetAdvertisedName(device);
            if (!string.IsNullOrWhiteSpace(advertisedName))
            {
                return advertisedName;
            }

            if (!string.IsNullOrWhiteSpace(device.Name))
            {
                return device.Name;
            }

            return "Feeder device";
        }

        public string GetDeviceIdentifier(IDevice device)
        {
            var rawId = device.Id.ToString("N");
            var suffix = rawId.Length >= 6 ? rawId[^6..].ToUpperInvariant() : rawId.ToUpperInvariant();
            return $"ID {suffix}";
        }

        public async Task<List<IDevice>> ScanForFeedersAsync(CancellationToken ct = default)
        {
            await EnsureBlePermissionsAsync();

            var devices = new List<IDevice>();

            foreach (var device in _adapter.GetSystemConnectedOrPairedDevices(new[] { ServiceUuid }))
            {
                AddIfMissing(devices, device);
            }

            // Unsubscribe any previous handler to prevent leaks
            if (_discoveredHandler != null)
            {
                _adapter.DeviceDiscovered -= _discoveredHandler;
            }

            _discoveredHandler = (s, a) =>
            {
                AddIfMissing(devices, a.Device);
            };
            _adapter.DeviceDiscovered += _discoveredHandler;

            try
            {
                await _adapter.StartScanningForDevicesAsync(cancellationToken: ct);
            }
            catch (System.OperationCanceledException)
            {
                // Scan was cancelled — return what we found
            }
            finally
            {
                _adapter.DeviceDiscovered -= _discoveredHandler;
                _discoveredHandler = null;
            }

            return devices;
        }

        private static void AddIfMissing(List<IDevice> devices, IDevice device)
        {
            if (devices.Any(d => d.Id == device.Id))
            {
                return;
            }

            if (IsFeederDevice(device))
            {
                devices.Add(device);
            }
        }

        private static bool IsFeederDevice(IDevice device)
        {
            if (device.Name?.Contains("Feeder", StringComparison.OrdinalIgnoreCase) == true)
            {
                return true;
            }

            var advertisedName = GetAdvertisedName(device);
            return advertisedName?.Contains("Feeder", StringComparison.OrdinalIgnoreCase) == true;
        }

        private static string? GetAdvertisedName(IDevice device)
        {
            foreach (var record in device.AdvertisementRecords)
            {
                if (record.Type != AdvertisementRecordType.CompleteLocalName &&
                    record.Type != AdvertisementRecordType.ShortLocalName)
                {
                    continue;
                }

                var advertisedName = Encoding.UTF8.GetString(record.Data);
                if (!string.IsNullOrWhiteSpace(advertisedName))
                {
                    return advertisedName.Trim();
                }
            }

            return null;
        }

        public async Task<string?> ProvisionDeviceAsync(IDevice device, string ssid, string password)
        {
            try
            {
                LogBle($"BLE provisioning start: id={device.Id} name={GetDisplayName(device)} state={device.State}");
                await EnsureBlePermissionsAsync();

                if (_adapter.IsScanning)
                {
                    LogBle("BLE provisioning: stopping active scan before connect");
                    await _adapter.StopScanningForDevicesAsync();
                }

                if (device.State != DeviceState.Disconnected)
                {
                    LogBle($"BLE provisioning: disconnecting stale state={device.State}");
                    await TryDisconnectAsync(device);
                    await Task.Delay(750);
                }

#if ANDROID
                return await ProvisionDeviceAndroidAsync(device, ssid, password);
#else
                await ConnectWithRetryAsync(device);
                LogBle($"BLE provisioning: connect finished state={device.State}");
                await Task.Delay(250);

                LogBle("BLE provisioning: discovering services");
                var services = await device.GetServicesAsync();
                LogBle($"BLE provisioning: discovered {services.Count} services");
                var service = services.FirstOrDefault(s => s.Id == ServiceUuid);

                if (service == null)
                    throw new InvalidOperationException("Feeder BLE service not found on device.");

                LogBle("BLE provisioning: resolving characteristics");
                var ssidChar = await service.GetCharacteristicAsync(SsidCharUuid);
                var passChar = await service.GetCharacteristicAsync(PassCharUuid);

                if (ssidChar == null || passChar == null)
                    throw new InvalidOperationException("Required BLE characteristics not found.");

                LogBle("BLE provisioning: writing SSID");
                await ssidChar.WriteAsync(Encoding.UTF8.GetBytes(ssid));
                LogBle("BLE provisioning: writing password");
                await passChar.WriteAsync(Encoding.UTF8.GetBytes(password));

                // Poll for IP address for up to 30 seconds
                var ipChar = await service.GetCharacteristicAsync(IpCharUuid);
                string? ip = null;

                if (ipChar != null)
                {
                    for (int i = 0; i < 30; i++)
                    {
                        LogBle($"BLE provisioning: reading IP attempt={i + 1}");
                        var bytes = await ipChar.ReadAsync();
                        if (bytes.data != null && bytes.data.Length > 0)
                        {
                            ip = Encoding.UTF8.GetString(bytes.data);
                            LogBle($"BLE provisioning: IP read returned '{ip}'");
                            if (!string.IsNullOrEmpty(ip) && ip != "0.0.0.0") break;
                        }
                        await Task.Delay(1000);
                    }
                }

                LogBle($"BLE provisioning complete: final IP='{ip ?? "<null>"}' state={device.State}");
                await TryDisconnectAsync(device);
                return ip;
#endif
            }
            catch (Exception ex)
            {
                LogBle($"BLE Provisioning Error: {ex}");
                await TryDisconnectAsync(device);
                throw;
            }
        }

#if ANDROID
        private async Task<string?> ProvisionDeviceAndroidAsync(IDevice device, string ssid, string password)
        {
            if (device.NativeDevice is not BluetoothDevice nativeDevice)
            {
                throw new InvalidOperationException("Native Android BLE device was not available.");
            }

            var connectDevice = ResolveConnectDevice(nativeDevice);
            LogBle($"BLE provisioning: native target address={connectDevice.Address} name={connectDevice.Name ?? "<null>"} type={connectDevice.Type} bondState={connectDevice.BondState}");
            await EnsureUnbondedAsync(connectDevice);

            using var connectLogger = new AndroidBleConnectLogger(connectDevice.Address);
            using var session = new AndroidBleProvisioningSession(connectDevice);

            await session.ConnectAsync();
            LogBle("BLE provisioning: native connect finished");

            await session.DiscoverServicesAsync();
            LogBle("BLE provisioning: native services discovered");

            await session.WriteCharacteristicAsync(ServiceUuid, SsidCharUuid, Encoding.UTF8.GetBytes(ssid));
            LogBle("BLE provisioning: native SSID write complete");

            await session.WriteCharacteristicAsync(ServiceUuid, PassCharUuid, Encoding.UTF8.GetBytes(password));
            LogBle("BLE provisioning: native password write complete");

            for (int attempt = 1; attempt <= 30; attempt++)
            {
                LogBle($"BLE provisioning: native IP read attempt={attempt}");
                var value = await session.ReadCharacteristicAsync(ServiceUuid, IpCharUuid);
                if (value.Length == 0)
                {
                    await Task.Delay(1000);
                    continue;
                }

                var ip = Encoding.UTF8.GetString(value).TrimEnd('\0');
                LogBle($"BLE provisioning: native IP read returned '{ip}'");
                if (!string.IsNullOrWhiteSpace(ip) && ip != "0.0.0.0")
                {
                    return ip;
                }

                await Task.Delay(1000);
            }

            return null;
        }

        private static BluetoothDevice ResolveConnectDevice(BluetoothDevice nativeDevice)
        {
            var adapter = BluetoothAdapter.DefaultAdapter;
            var address = nativeDevice.Address;
            if (adapter == null || string.IsNullOrWhiteSpace(address))
            {
                return nativeDevice;
            }

            try
            {
                return adapter.GetRemoteDevice(address) ?? nativeDevice;
            }
            catch
            {
                return nativeDevice;
            }
        }

        private static async Task EnsureUnbondedAsync(BluetoothDevice device)
        {
            if (device.BondState == Bond.None)
            {
                return;
            }

            LogBle($"BLE provisioning: clearing stale bond state={device.BondState} address={device.Address}");

            if (device.BondState == Bond.Bonding)
            {
                TryInvokeBondMethod(device, "cancelBondProcess");
                await Task.Delay(500);
            }

            if (!TryInvokeBondMethod(device, "removeBond"))
            {
                LogBle("BLE provisioning: removeBond not available or failed; continuing with existing bond state");
                return;
            }

            for (var attempt = 0; attempt < 20; attempt++)
            {
                await Task.Delay(500);
                var refreshedDevice = ResolveConnectDevice(device);
                LogBle($"BLE provisioning: bond poll attempt={attempt + 1} state={refreshedDevice.BondState} address={refreshedDevice.Address}");
                if (refreshedDevice.BondState == Bond.None)
                {
                    return;
                }
            }

            LogBle($"BLE provisioning: bond did not clear before connect; final state={ResolveConnectDevice(device).BondState}");
        }

        private static bool TryInvokeBondMethod(BluetoothDevice device, string methodName)
        {
            try
            {
                var javaClass = device.Class;
                var method = javaClass?.GetMethod(methodName);
                if (method == null)
                {
                    return false;
                }

                var result = method.Invoke(device);
                return result is null || result.ToString()?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
            }
            catch (Exception ex)
            {
                LogBle($"BLE provisioning: {methodName} failed: {ex.Message}");
                return false;
            }
        }

        private sealed class AndroidBleConnectLogger : BroadcastReceiver, IDisposable
        {
            private readonly Context _context;
            private readonly string _address;
            private bool _disposed;

            public AndroidBleConnectLogger(string address)
            {
                _context = Application.Context;
                _address = address;

                var filter = new IntentFilter();
                filter.AddAction(BluetoothDevice.ActionAclConnected);
                filter.AddAction(BluetoothDevice.ActionAclDisconnected);
                filter.AddAction(BluetoothDevice.ActionAclDisconnectRequested);
                filter.AddAction(BluetoothDevice.ActionBondStateChanged);
                filter.AddAction(BluetoothDevice.ActionPairingRequest);
                if (OperatingSystem.IsAndroidVersionAtLeast(36))
                {
                    filter.AddAction(BluetoothDevice.ActionKeyMissing);
                }

                if (OperatingSystem.IsAndroidVersionAtLeast(33))
                {
                    _context.RegisterReceiver(this, filter, ReceiverFlags.NotExported);
                }
                else
                {
                    _context.RegisterReceiver(this, filter);
                }

                LogBle($"BLE provisioning: registered Android BLE broadcast logger for address={address}");
            }

            public override void OnReceive(Context? context, Intent? intent)
            {
                if (_disposed || intent == null)
                {
                    return;
                }

                var device = intent.GetParcelableExtra(BluetoothDevice.ExtraDevice, Java.Lang.Class.FromType(typeof(BluetoothDevice))) as BluetoothDevice;
                var address = device?.Address;
                if (!string.Equals(address, _address, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                var action = intent.Action ?? "<unknown>";
                var transport = intent.GetIntExtra(BluetoothDevice.ExtraTransport, int.MinValue);
                var bondState = intent.GetIntExtra(BluetoothDevice.ExtraBondState, int.MinValue);
                var previousBondState = intent.GetIntExtra(BluetoothDevice.ExtraPreviousBondState, int.MinValue);
                var pairingVariant = intent.GetIntExtra(BluetoothDevice.ExtraPairingVariant, int.MinValue);
                var pairingKey = intent.GetIntExtra(BluetoothDevice.ExtraPairingKey, int.MinValue);
                LogBle($"BLE broadcast action={action} address={address} transport={transport} bond={bondState} prevBond={previousBondState} pairingVariant={pairingVariant} pairingKey={pairingKey}");
            }

            public new void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;

                try
                {
                    _context.UnregisterReceiver(this);
                }
                catch
                {
                }
            }
        }
#endif

        private async Task ConnectWithRetryAsync(IDevice device)
        {
            if (device.State == DeviceState.Connected)
            {
                return;
            }

            try
            {
                LogBle($"BLE connect attempt 1: id={device.Id} name={GetDisplayName(device)} state={device.State}");
                await _adapter.ConnectToDeviceAsync(device, DirectConnectParameters);
            }
            catch (DeviceConnectionException ex)
            {
                LogBle($"BLE connect attempt 1 failed: {ex}; state={device.State}");
                await TryDisconnectAsync(device);
                await Task.Delay(1000);
                LogBle($"BLE connect attempt 2: id={device.Id} name={GetDisplayName(device)} state={device.State}");
                await _adapter.ConnectToDeviceAsync(device, DirectConnectParameters);
            }
        }

        private async Task TryDisconnectAsync(IDevice device)
        {
            if (device.State == DeviceState.Disconnected)
            {
                return;
            }

            try
            {
                await _adapter.DisconnectDeviceAsync(device);
            }
            catch
            {
            }
        }

        private static async Task EnsureBlePermissionsAsync()
        {
#if ANDROID
            if (OperatingSystem.IsAndroidVersionAtLeast(31))
            {
                var scanStatus = await Permissions.CheckStatusAsync<BluetoothScanPermission>();
                if (scanStatus != PermissionStatus.Granted)
                {
                    scanStatus = await Permissions.RequestAsync<BluetoothScanPermission>();
                }

                var connectStatus = await Permissions.CheckStatusAsync<BluetoothConnectPermission>();
                if (connectStatus != PermissionStatus.Granted)
                {
                    connectStatus = await Permissions.RequestAsync<BluetoothConnectPermission>();
                }

                if (scanStatus != PermissionStatus.Granted || connectStatus != PermissionStatus.Granted)
                {
                    throw new InvalidOperationException("Bluetooth permissions are required to scan and connect to feeders.");
                }
            }
            else
            {
                var locationStatus = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (locationStatus != PermissionStatus.Granted)
                {
                    locationStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                }

                if (locationStatus != PermissionStatus.Granted)
                {
                    throw new InvalidOperationException("Location permission is required to scan for feeders on this Android version.");
                }
            }
#endif
        }

#if ANDROID
        private sealed class BluetoothScanPermission : Permissions.BasePlatformPermission
        {
            public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
                new[] { (global::Android.Manifest.Permission.BluetoothScan, true) };
        }

        private sealed class BluetoothConnectPermission : Permissions.BasePlatformPermission
        {
            public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
                new[] { (global::Android.Manifest.Permission.BluetoothConnect, true) };
        }

        private sealed class AndroidBleProvisioningSession : BluetoothGattCallback, IDisposable
        {
            private const int Le1mPhyMask = 1;
            private readonly BluetoothDevice _device;
            private readonly Context _context;
            private readonly Handler _callbackHandler;
            private readonly ConcurrentDictionary<string, TaskCompletionSource<byte[]>> _readOperations = new();
            private readonly ConcurrentDictionary<string, TaskCompletionSource<bool>> _writeOperations = new();
            private TaskCompletionSource<bool>? _connectTcs;
            private TaskCompletionSource<bool>? _servicesTcs;
            private BluetoothGatt? _gatt;
            private bool _disposed;

            public AndroidBleProvisioningSession(BluetoothDevice device)
            {
                _device = device;
                _context = Android.App.Application.Context;
                _callbackHandler = new Handler(Looper.MainLooper!);
            }

            public async Task ConnectAsync()
            {
                Exception? lastError = null;

                foreach (var attempt in GetConnectAttempts())
                {
                    CleanupGatt();
                    _connectTcs = NewBoolTcs();

                    LogBle($"BLE native connect attempt={attempt.Name} autoConnect={attempt.AutoConnect} address={_device.Address}");
                    _gatt = await OpenGattAsync(attempt);
                    if (_gatt == null)
                    {
                        lastError = new InvalidOperationException($"Android BluetoothGatt connection could not be created for attempt '{attempt.Name}'.");
                        continue;
                    }

                    try
                    {
                        _ = KickConnectIfStalledAsync(attempt);
                        await WaitAsync(_connectTcs.Task, attempt.Timeout, $"native connect ({attempt.Name})");
                        return;
                    }
                    catch (Exception ex)
                    {
                        lastError = ex;
                        LogBle($"BLE native connect attempt failed: name={attempt.Name} error={ex.Message}");
                        await Task.Delay(750);
                    }
                }

                throw new InvalidOperationException("Android BluetoothGatt connection failed before a usable callback sequence was established.", lastError);
            }

            public async Task DiscoverServicesAsync()
            {
                if (_gatt == null)
                {
                    throw new InvalidOperationException("BluetoothGatt is not connected.");
                }

                _servicesTcs = NewBoolTcs();
                if (!_gatt.DiscoverServices())
                {
                    throw new InvalidOperationException("BluetoothGatt.DiscoverServices returned false.");
                }

                await WaitAsync(_servicesTcs.Task, TimeSpan.FromSeconds(10), "service discovery");
            }

            public async Task WriteCharacteristicAsync(Guid serviceUuid, Guid characteristicUuid, byte[] value)
            {
                var characteristic = GetCharacteristic(serviceUuid, characteristicUuid);
                var key = characteristic.Uuid?.ToString() ?? characteristicUuid.ToString();
                var operation = NewBoolTcs();
                _writeOperations[key] = operation;

                characteristic.WriteType = GattWriteType.Default;
                if (!characteristic.SetValue(value))
                {
                    _writeOperations.TryRemove(key, out _);
                    throw new InvalidOperationException($"Failed to set value for characteristic {characteristicUuid}.");
                }

                if (!_gatt!.WriteCharacteristic(characteristic))
                {
                    _writeOperations.TryRemove(key, out _);
                    throw new InvalidOperationException($"BluetoothGatt.WriteCharacteristic returned false for {characteristicUuid}.");
                }

                await WaitAsync(operation.Task, TimeSpan.FromSeconds(10), $"write {characteristicUuid}");
            }

            public async Task<byte[]> ReadCharacteristicAsync(Guid serviceUuid, Guid characteristicUuid)
            {
                var characteristic = GetCharacteristic(serviceUuid, characteristicUuid);
                var key = characteristic.Uuid?.ToString() ?? characteristicUuid.ToString();
                var operation = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
                _readOperations[key] = operation;

                if (!_gatt!.ReadCharacteristic(characteristic))
                {
                    _readOperations.TryRemove(key, out _);
                    throw new InvalidOperationException($"BluetoothGatt.ReadCharacteristic returned false for {characteristicUuid}.");
                }

                return await WaitAsync(operation.Task, TimeSpan.FromSeconds(10), $"read {characteristicUuid}");
            }

            public override void OnConnectionStateChange(BluetoothGatt? gatt, GattStatus status, ProfileState newState)
            {
                LogBle($"BLE native onConnectionStateChange status={status} newState={newState}");

                if (status == GattStatus.Success && newState == ProfileState.Connected)
                {
                    gatt?.RequestConnectionPriority(GattConnectionPriority.High);
                    _connectTcs?.TrySetResult(true);
                    return;
                }

                if (newState == ProfileState.Disconnected)
                {
                    var exception = new InvalidOperationException($"Native BLE disconnected: status={status} state={newState}");
                    _connectTcs?.TrySetException(exception);
                    _servicesTcs?.TrySetException(exception);
                    FailPendingOperations(exception);
                }
            }

            public override void OnServicesDiscovered(BluetoothGatt? gatt, GattStatus status)
            {
                LogBle($"BLE native onServicesDiscovered status={status}");
                if (status == GattStatus.Success)
                {
                    _servicesTcs?.TrySetResult(true);
                    return;
                }

                _servicesTcs?.TrySetException(new InvalidOperationException($"Service discovery failed: status={status}"));
            }

            public override void OnCharacteristicWrite(BluetoothGatt? gatt, BluetoothGattCharacteristic? characteristic, GattStatus status)
            {
                var key = characteristic?.Uuid?.ToString();
                LogBle($"BLE native onCharacteristicWrite uuid={key} status={status}");
                if (key == null || !_writeOperations.TryRemove(key, out var operation))
                {
                    return;
                }

                if (status == GattStatus.Success)
                {
                    operation.TrySetResult(true);
                    return;
                }

                operation.TrySetException(new InvalidOperationException($"Characteristic write failed: uuid={key} status={status}"));
            }

            public override void OnCharacteristicRead(BluetoothGatt? gatt, BluetoothGattCharacteristic? characteristic, GattStatus status)
            {
                var key = characteristic?.Uuid?.ToString();
                LogBle($"BLE native onCharacteristicRead uuid={key} status={status}");
                if (key == null || !_readOperations.TryRemove(key, out var operation))
                {
                    return;
                }

                if (status == GattStatus.Success)
                {
                    operation.TrySetResult(characteristic?.GetValue() ?? Array.Empty<byte>());
                    return;
                }

                operation.TrySetException(new InvalidOperationException($"Characteristic read failed: uuid={key} status={status}"));
            }

            public new void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                CleanupGatt();
            }

            private IEnumerable<ConnectAttempt> GetConnectAttempts()
            {
                if (OperatingSystem.IsAndroidVersionAtLeast(26))
                {
                    yield return new ConnectAttempt("transport-le-handler", AutoConnect: false, Timeout: TimeSpan.FromSeconds(10));
                    yield return new ConnectAttempt("transport-auto-handler", AutoConnect: false, Timeout: TimeSpan.FromSeconds(10), UseAutoTransport: true);
                }

                if (OperatingSystem.IsAndroidVersionAtLeast(23))
                {
                    yield return new ConnectAttempt("transport-le", AutoConnect: false, Timeout: TimeSpan.FromSeconds(10));
                }
                else
                {
                    yield return new ConnectAttempt("legacy", AutoConnect: false, Timeout: TimeSpan.FromSeconds(10));
                }

                yield return new ConnectAttempt("auto-connect-handler", AutoConnect: true, Timeout: TimeSpan.FromSeconds(15), UseAutoTransport: OperatingSystem.IsAndroidVersionAtLeast(26));
            }

            private Task<BluetoothGatt?> OpenGattAsync(ConnectAttempt attempt)
            {
                return MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (OperatingSystem.IsAndroidVersionAtLeast(26) && attempt.UseHandler)
                    {
                        var transport = attempt.UseAutoTransport ? BluetoothTransports.Auto : BluetoothTransports.Le;
                        return _device.ConnectGatt(_context, attempt.AutoConnect, this, transport, (global::Android.Bluetooth.LE.ScanSettingsPhy)Le1mPhyMask, _callbackHandler);
                    }

                    if (OperatingSystem.IsAndroidVersionAtLeast(23))
                    {
                        var transport = attempt.UseAutoTransport ? BluetoothTransports.Auto : BluetoothTransports.Le;
                        return _device.ConnectGatt(_context, attempt.AutoConnect, this, transport);
                    }

                    return _device.ConnectGatt(_context, attempt.AutoConnect, this);
                });
            }

            private async Task KickConnectIfStalledAsync(ConnectAttempt attempt)
            {
                if (attempt.AutoConnect)
                {
                    return;
                }

                try
                {
                    await Task.Delay(1500);
                    if (_connectTcs?.Task.IsCompleted != false)
                    {
                        return;
                    }

                    var gatt = _gatt;
                    if (gatt == null)
                    {
                        return;
                    }

                    var result = gatt.Connect();
                    LogBle($"BLE native connect fallback: name={attempt.Name} connect() returned={result}");
                }
                catch (Exception ex)
                {
                    LogBle($"BLE native connect fallback failed: name={attempt.Name} error={ex.Message}");
                }
            }

            private void CleanupGatt()
            {
                try
                {
                    _gatt?.Disconnect();
                }
                catch
                {
                }

                try
                {
                    _gatt?.Close();
                }
                catch
                {
                }

                _gatt = null;
            }

            private BluetoothGattCharacteristic GetCharacteristic(Guid serviceUuid, Guid characteristicUuid)
            {
                if (_gatt == null)
                {
                    throw new InvalidOperationException("BluetoothGatt is not available.");
                }

                var service = _gatt.GetService(UUID.FromString(serviceUuid.ToString()));
                if (service == null)
                {
                    throw new InvalidOperationException($"Service {serviceUuid} was not found.");
                }

                var characteristic = service.GetCharacteristic(UUID.FromString(characteristicUuid.ToString()));
                if (characteristic == null)
                {
                    throw new InvalidOperationException($"Characteristic {characteristicUuid} was not found.");
                }

                return characteristic;
            }

            private static TaskCompletionSource<bool> NewBoolTcs()
            {
                return new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            private static async Task WaitAsync(Task task, TimeSpan timeout, string operation)
            {
                using var timeoutCts = new CancellationTokenSource(timeout);
                var completed = await Task.WhenAny(task, Task.Delay(Timeout.InfiniteTimeSpan, timeoutCts.Token));
                if (completed != task)
                {
                    throw new TimeoutException($"Timed out during {operation}.");
                }

                await task;
            }

            private static async Task<T> WaitAsync<T>(Task<T> task, TimeSpan timeout, string operation)
            {
                using var timeoutCts = new CancellationTokenSource(timeout);
                var completed = await Task.WhenAny(task, Task.Delay(Timeout.InfiniteTimeSpan, timeoutCts.Token));
                if (completed != task)
                {
                    throw new TimeoutException($"Timed out during {operation}.");
                }

                return await task;
            }

            private void FailPendingOperations(Exception exception)
            {
                foreach (var operation in _writeOperations.Values)
                {
                    operation.TrySetException(exception);
                }

                foreach (var operation in _readOperations.Values)
                {
                    operation.TrySetException(exception);
                }

                _writeOperations.Clear();
                _readOperations.Clear();
            }

            private sealed record ConnectAttempt(string Name, bool AutoConnect, TimeSpan Timeout, bool UseAutoTransport = false, bool UseHandler = true);
        }
#endif
    }
}
