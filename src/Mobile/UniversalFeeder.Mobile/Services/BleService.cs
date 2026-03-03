using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
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
            var devices = new List<IDevice>();

            // Unsubscribe any previous handler to prevent leaks
            if (_discoveredHandler != null)
            {
                _adapter.DeviceDiscovered -= _discoveredHandler;
            }

            _discoveredHandler = (s, a) =>
            {
                if (a.Device.Name?.Contains("Feeder") == true &&
                    !devices.Any(d => d.Id == a.Device.Id))
                {
                    devices.Add(a.Device);
                }
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

        public async Task<string?> ProvisionDeviceAsync(IDevice device, string ssid, string password)
        {
            try
            {
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
    }
}
