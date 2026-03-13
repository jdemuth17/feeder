using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Microsoft.Maui.ApplicationModel;
using System.Text;

namespace UniversalFeeder.Mobile.Services
{
    public class BleService
    {
        private static readonly Guid ServiceUuid = Guid.Parse("4fafc201-1fb5-459e-8fcc-c5c9c331914b");
        private static readonly Guid SsidCharUuid = Guid.Parse("beb5483e-36e1-4688-b7f5-ea07361b26a8");
        private static readonly Guid PassCharUuid = Guid.Parse("d6e98ba1-8ef4-4594-ba04-0390ea000001");
        private static readonly Guid IpCharUuid = Guid.Parse("e2a00001-8ef4-4594-ba04-0390ea000001");

        private readonly IAdapter _adapter;
        private readonly IBluetoothLE _bluetooth;
        private EventHandler<Plugin.BLE.Abstractions.EventArgs.DeviceEventArgs>? _discoveredHandler;

        public BleService()
        {
            _bluetooth = CrossBluetoothLE.Current;
            _adapter = CrossBluetoothLE.Current.Adapter;
            _adapter.ScanTimeout = 10000; // 10 second scan timeout
        }

        public bool IsBluetoothOn => _bluetooth.IsOn;

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
            catch (OperationCanceledException)
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

            foreach (var record in device.AdvertisementRecords)
            {
                if (record.Type != AdvertisementRecordType.CompleteLocalName &&
                    record.Type != AdvertisementRecordType.ShortLocalName)
                {
                    continue;
                }

                var advertisedName = Encoding.UTF8.GetString(record.Data);
                if (advertisedName.Contains("Feeder", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public async Task<string?> ProvisionDeviceAsync(IDevice device, string ssid, string password)
        {
            try
            {
                await EnsureBlePermissionsAsync();

                await _adapter.ConnectToDeviceAsync(device);
                var services = await device.GetServicesAsync();
                var service = services.FirstOrDefault(s => s.Id == ServiceUuid);

                if (service == null)
                    throw new InvalidOperationException("Feeder BLE service not found on device.");

                var ssidChar = await service.GetCharacteristicAsync(SsidCharUuid);
                var passChar = await service.GetCharacteristicAsync(PassCharUuid);

                if (ssidChar == null || passChar == null)
                    throw new InvalidOperationException("Required BLE characteristics not found.");

                await ssidChar.WriteAsync(Encoding.UTF8.GetBytes(ssid));
                await passChar.WriteAsync(Encoding.UTF8.GetBytes(password));

                // Poll for IP address for up to 30 seconds
                var ipChar = await service.GetCharacteristicAsync(IpCharUuid);
                string? ip = null;

                if (ipChar != null)
                {
                    for (int i = 0; i < 30; i++)
                    {
                        var bytes = await ipChar.ReadAsync();
                        if (bytes.data != null && bytes.data.Length > 0)
                        {
                            ip = Encoding.UTF8.GetString(bytes.data);
                            if (!string.IsNullOrEmpty(ip) && ip != "0.0.0.0") break;
                        }
                        await Task.Delay(1000);
                    }
                }

                await _adapter.DisconnectDeviceAsync(device);
                return ip;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"BLE Provisioning Error: {ex.Message}");
                try { await _adapter.DisconnectDeviceAsync(device); } catch { }
                throw;
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
#endif
    }
}
