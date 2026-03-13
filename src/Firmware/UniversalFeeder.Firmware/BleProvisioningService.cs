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
            Console.WriteLine("BLE: Getting BluetoothLEServer instance...");
            _server = BluetoothLEServer.Instance;
            Console.WriteLine($"BLE: Server instance acquired, setting name to: {deviceName}");
            _server.DeviceName = deviceName;

            Console.WriteLine("BLE: Creating GATT Service Provider...");
            var serviceResult = GattServiceProvider.Create(new Guid(_serviceUuid));
            if (serviceResult.Error != BluetoothError.Success)
            {
                Console.WriteLine($"Failed to create GATT Service Provider: {serviceResult.Error}");
                return;
            }
            Console.WriteLine("BLE: GATT Service Provider created");

            _serviceProvider = serviceResult.ServiceProvider;

            // SSID Characteristic
            var ssidResult = _serviceProvider.Service.CreateCharacteristic(
                new Guid(_ssidUuid),
                new GattLocalCharacteristicParameters
                {
                    CharacteristicProperties = GattCharacteristicProperties.Write,
                    UserDescription = "Wi-Fi SSID"
                });
            _ssidCharacteristic = ssidResult.Characteristic;
            _ssidCharacteristic.WriteRequested += OnSsidWriteRequested;

            // Password Characteristic
            var passResult = _serviceProvider.Service.CreateCharacteristic(
                new Guid(_passUuid),
                new GattLocalCharacteristicParameters
                {
                    CharacteristicProperties = GattCharacteristicProperties.Write,
                    UserDescription = "Wi-Fi Password"
                });
            _passCharacteristic = passResult.Characteristic;
            _passCharacteristic.WriteRequested += OnPassWriteRequested;

            // IP Address Characteristic (Read/Notify)
            var ipResult = _serviceProvider.Service.CreateCharacteristic(
                new Guid(_ipUuid),
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
            var bytes = Encoding.UTF8.GetBytes(ip);
            var buffer = new Buffer(bytes);
            _ipCharacteristic.NotifyValue(buffer);
            Console.WriteLine($"BLE IP Updated: {ip}");
#endif
        }

#if NANOFRAMEWORK
        private void OnSsidWriteRequested(GattLocalCharacteristic sender, GattWriteRequestedEventArgs e)
        {
            var request = e.GetRequest();
            var reader = DataReader.FromBuffer(request.Value);
            var data = new byte[reader.UnconsumedBufferLength];
            reader.ReadBytes(data);
            Ssid = Encoding.UTF8.GetString(data, 0, data.Length);
            Console.WriteLine($"SSID Received: {Ssid}");
            
            if (CredentialsReceived) OnCredentialsReceived?.Invoke(this, EventArgs.Empty);
        }

        private void OnPassWriteRequested(GattLocalCharacteristic sender, GattWriteRequestedEventArgs e)
        {
            var request = e.GetRequest();
            var reader = DataReader.FromBuffer(request.Value);
            var data = new byte[reader.UnconsumedBufferLength];
            reader.ReadBytes(data);
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
