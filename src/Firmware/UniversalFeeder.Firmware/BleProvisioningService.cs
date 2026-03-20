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
        private readonly string _idUuid = "f4b00001-8ef4-4594-ba04-0390ea000001";

#if NANOFRAMEWORK
        private BluetoothLEServer _server;
        private GattServiceProvider _serviceProvider;
        private GattLocalCharacteristic _ssidCharacteristic;
        private GattLocalCharacteristic _passCharacteristic;
        private GattLocalCharacteristic _ipCharacteristic;
        private GattLocalCharacteristic _idCharacteristic;
#endif

        public string Ssid { get; private set; }
        public string Password { get; private set; }
        public string IpAddress { get; private set; } = "0.0.0.0";
        public string DeviceId { get; private set; } = "Unknown";
        public bool CredentialsReceived => !string.IsNullOrEmpty(Ssid) && !string.IsNullOrEmpty(Password);

        public event EventHandler OnCredentialsReceived;

        public void Start(string deviceName, string deviceId)
        {
            DeviceId = deviceId;
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
            _ipCharacteristic.ReadRequested += OnIpReadRequested;

            // Device ID Characteristic (Read)
            var idResult = _serviceProvider.Service.CreateCharacteristic(
                new Guid(_idUuid),
                new GattLocalCharacteristicParameters
                {
                    CharacteristicProperties = GattCharacteristicProperties.Read,
                    UserDescription = "Unique Device ID"
                });
            _idCharacteristic = idResult.Characteristic;
            _idCharacteristic.ReadRequested += OnIdReadRequested;

            _serviceProvider.StartAdvertising(new GattServiceProviderAdvertisingParameters
            {
                IsDiscoverable = true,
                IsConnectable = true,
                ServiceUuid = new Guid(_serviceUuid) // Important for filtering
            });

            Console.WriteLine($"BLE Provisioning Server Started: {deviceName}");
#endif
        }

        public void UpdateIpAddress(string ip)
        {
            IpAddress = ip;
#if NANOFRAMEWORK
            var bytes = Encoding.UTF8.GetBytes(ip);
            var buffer = new Buffer(bytes);
            _ipCharacteristic.NotifyValue(buffer);
            Console.WriteLine($"BLE IP Updated: {ip}");
#endif
        }

#if NANOFRAMEWORK
        private void OnIpReadRequested(GattLocalCharacteristic sender, GattReadRequestedEventArgs e)
        {
            var request = e.GetRequest();
            var bytes = Encoding.UTF8.GetBytes(IpAddress);
            request.RespondWithValue(new Buffer(bytes));
            Console.WriteLine($"IP Read Requested: {IpAddress}");
        }

        private void OnIdReadRequested(GattLocalCharacteristic sender, GattReadRequestedEventArgs e)
        {
            var request = e.GetRequest();
            var bytes = Encoding.UTF8.GetBytes(DeviceId);
            request.RespondWithValue(new Buffer(bytes));
            Console.WriteLine($"Device ID Read Requested: {DeviceId}");
        }

        private void OnSsidWriteRequested(GattLocalCharacteristic sender, GattWriteRequestedEventArgs e)
        {
            var request = e.GetRequest();
            var reader = DataReader.FromBuffer(request.Value);
            var data = new byte[reader.UnconsumedBufferLength];
            reader.ReadBytes(data);
            Ssid = Encoding.UTF8.GetString(data, 0, data.Length);
            Console.WriteLine($"SSID Received: {Ssid}");
            
            request.Respond(); // Critical fix
            
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

            request.Respond(); // Critical fix

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
