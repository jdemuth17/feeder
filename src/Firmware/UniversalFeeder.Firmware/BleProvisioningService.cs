using System;
using System.Text;
#if NANOFRAMEWORK
using nanoFramework.Device.Bluetooth;
using nanoFramework.Device.Bluetooth.GenericAttributeProfile;
#endif

namespace UniversalFeeder.Firmware
{
    public class BleProvisioningService : IDisposable
    {
        private readonly string _serviceUuid = "4fafc201-1fb5-459e-8fcc-c5c9c331914b";
        private readonly string _ssidUuid = "beb5483e-36e1-4688-b7f5-ea07361b26a8";
        private readonly string _passUuid = "d6e98ba1-8ef4-4594-ba04-0390ea000001";
        private readonly string _ipUuid = "e2a00001-8ef4-4594-ba04-0390ea000001";

#if NANOFRAMEWORK
        private BluetoothLEServer _server;
        private GattServiceProvider _serviceProvider;
        private GattLocalCharacteristic _ssidCharacteristic;
        private GattLocalCharacteristic _passCharacteristic;
        private GattLocalCharacteristic _ipCharacteristic;
#endif

        public string Ssid { get; private set; }
        public string Password { get; private set; }
        public bool CredentialsReceived => !string.IsNullOrEmpty(Ssid) && !string.IsNullOrEmpty(Password);

        public event EventHandler OnCredentialsReceived;

        public void Start(string deviceName)
        {
#if NANOFRAMEWORK
            _server = BluetoothLEServer.Instance;
            _server.DeviceName = deviceName;

            var serviceResult = GattServiceProvider.Create(Guid.Parse(_serviceUuid));
            if (serviceResult.Error != BluetoothError.Success)
            {
                Console.WriteLine("Failed to create GATT Service Provider");
                return;
            }

            _serviceProvider = serviceResult.ServiceProvider;

            // SSID Characteristic
            var ssidResult = _serviceProvider.Service.CreateCharacteristic(
                Guid.Parse(_ssidUuid),
                new GattLocalCharacteristicParameters
                {
                    CharacteristicProperties = GattCharacteristicProperties.Write,
                    UserDescription = "Wi-Fi SSID"
                });
            _ssidCharacteristic = ssidResult.Characteristic;
            _ssidCharacteristic.WriteRequested += OnSsidWriteRequested;

            // Password Characteristic
            var passResult = _serviceProvider.Service.CreateCharacteristic(
                Guid.Parse(_passUuid),
                new GattLocalCharacteristicParameters
                {
                    CharacteristicProperties = GattCharacteristicProperties.Write,
                    UserDescription = "Wi-Fi Password"
                });
            _passCharacteristic = passResult.Characteristic;
            _passCharacteristic.WriteRequested += OnPassWriteRequested;

            // IP Address Characteristic (Read/Notify)
            var ipResult = _serviceProvider.Service.CreateCharacteristic(
                Guid.Parse(_ipUuid),
                new GattLocalCharacteristicParameters
                {
                    CharacteristicProperties = GattCharacteristicProperties.Read | GattCharacteristicProperties.Notify,
                    UserDescription = "Assigned IP Address"
                });
            _ipCharacteristic = ipResult.Characteristic;

            _serviceProvider.StartAdvertising(new GattServiceProviderAdvertisingParameters
            {
                IsDiscoverable = true,
                IsConnectable = true
            });

            Console.WriteLine($"BLE Provisioning Server Started: {deviceName}");
#endif
        }

        public void UpdateIpAddress(string ip)
        {
#if NANOFRAMEWORK
            var buffer = Encoding.UTF8.GetBytes(ip);
            _ipCharacteristic.StaticValue = buffer;
            _ipCharacteristic.NotifyValue(buffer);
            Console.WriteLine($"BLE IP Updated: {ip}");
#endif
        }

#if NANOFRAMEWORK
        private void OnSsidWriteRequested(GattLocalCharacteristic sender, GattWriteRequestedEventArgs e)
        {
            var request = e.GetRequest();
            var data = request.Value.ToArray();
            Ssid = Encoding.UTF8.GetString(data, 0, data.Length);
            Console.WriteLine($"SSID Received: {Ssid}");
            
            if (CredentialsReceived) OnCredentialsReceived?.Invoke(this, EventArgs.Empty);
        }

        private void OnPassWriteRequested(GattLocalCharacteristic sender, GattWriteRequestedEventArgs e)
        {
            var request = e.GetRequest();
            var data = request.Value.ToArray();
            Password = Encoding.UTF8.GetString(data, 0, data.Length);
            Console.WriteLine("Password Received");

            if (CredentialsReceived) OnCredentialsReceived?.Invoke(this, EventArgs.Empty);
        }
#endif

        public void Stop()
        {
#if NANOFRAMEWORK
            _serviceProvider?.StopAdvertising();
            Console.WriteLine("BLE Provisioning Server Stopped");
#endif
        }

        public void Dispose()
        {
            Stop();
#if NANOFRAMEWORK
            _server?.Dispose();
#endif
        }
    }
}
