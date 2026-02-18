using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using System.Text;

namespace UniversalFeeder.Mobile.Services
{
    public class BleService
    {
        private readonly IAdapter _adapter;
        private readonly IBluetoothLE _bluetooth;

        public BleService()
        {
            _bluetooth = CrossBluetoothLE.Current;
            _adapter = CrossBluetoothLE.Current.Adapter;
        }

        public async Task<List<IDevice>> ScanForFeedersAsync()
        {
            var devices = new List<IDevice>();
            _adapter.DeviceDiscovered += (s, a) =>
            {
                if (a.Device.Name?.Contains("Feeder") == true)
                {
                    devices.Add(a.Device);
                }
            };

            await _adapter.StartScanningForDevicesAsync();
            return devices;
        }

        public async Task<bool> ProvisionDeviceAsync(IDevice device, string ssid, string password)
        {
            try
            {
                await _adapter.ConnectToDeviceAsync(device);
                var services = await device.GetServicesAsync();
                var service = services.FirstOrDefault(s => s.Id.ToString() == "4fafc201-1fb5-459e-8fcc-c5c9c331914b");
                
                if (service == null) return false;

                var ssidChar = await service.GetCharacteristicAsync(Guid.Parse("beb5483e-36e1-4688-b7f5-ea07361b26a8"));
                var passChar = await service.GetCharacteristicAsync(Guid.Parse("d6e98ba1-8ef4-4594-ba04-0390ea000001"));

                if (ssidChar != null) await ssidChar.WriteAsync(Encoding.UTF8.GetBytes(ssid));
                if (passChar != null) await passChar.WriteAsync(Encoding.UTF8.GetBytes(password));

                // Wait for IP characteristic to be updated
                var ipChar = await service.GetCharacteristicAsync(Guid.Parse("e2a00001-8ef4-4594-ba04-0390ea000001"));
                string ip = null;
                
                if (ipChar != null)
                {
                    // Poll for IP for up to 30 seconds
                    for (int i = 0; i < 30; i++)
                    {
                        var bytes = await ipChar.ReadAsync();
                        if (bytes != null && bytes.Length > 0)
                        {
                            ip = Encoding.UTF8.GetString(bytes);
                            if (ip != "0.0.0.0") break;
                        }
                        await Task.Delay(1000);
                    }
                }

                await _adapter.DisconnectDeviceAsync(device);
                return !string.IsNullOrEmpty(ip);
            }
            catch
            {
                return false;
            }
        }
    }
}
