using System.Collections.ObjectModel;
using System.Windows.Input;
using Plugin.BLE.Abstractions.Contracts;
using UniversalFeeder.Mobile.Services;

namespace UniversalFeeder.Mobile.ViewModels
{
    public class ProvisioningViewModel : BindableObject
    {
        private readonly BleService _bleService;
        private bool _isScanning;
        private IDevice _selectedDevice;
        private string _ssid;
        private string _password;
        private string _status;

        public ObservableCollection<IDevice> Devices { get; } = new();

        public bool IsScanning
        {
            get => _isScanning;
            set { _isScanning = value; OnPropertyChanged(); }
        }

        public IDevice SelectedDevice
        {
            get => _selectedDevice;
            set { _selectedDevice = value; OnPropertyChanged(); }
        }

        public string Ssid
        {
            get => _ssid;
            set { _ssid = value; OnPropertyChanged(); }
        }

        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(); }
        }

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public ICommand ScanCommand { get; }
        public ICommand ProvisionCommand { get; }

        public ProvisioningViewModel()
        {
            _bleService = new BleService();
            ScanCommand = new Command(async () => await ScanAsync());
            ProvisionCommand = new Command(async () => await ProvisionAsync());
        }

        private async Task ScanAsync()
        {
            IsScanning = true;
            Status = "Scanning...";
            Devices.Clear();
            var found = await _bleService.ScanForFeedersAsync();
            foreach (var d in found) Devices.Add(d);
            IsScanning = false;
            Status = found.Any() ? "Devices found." : "No feeders found.";
        }

        private async Task ProvisionAsync()
        {
            if (SelectedDevice == null || string.IsNullOrEmpty(Ssid))
            {
                Status = "Select a device and enter SSID.";
                return;
            }

            Status = "Provisioning via BLE...";
            string ip = await _bleService.ProvisionDeviceAndGetIpAsync(SelectedDevice, Ssid, Password);
            
            if (string.IsNullOrEmpty(ip))
            {
                Status = "Provisioning Failed (No IP).";
                return;
            }

            Status = $"BLE Success (IP: {ip}). Registering with Server...";

            try
            {
                using var client = new HttpClient();
                // Note: In a real scenario, this URL would be configurable
                var response = await client.PostAsJsonAsync("http://localhost:5000/api/feeders/register", new
                {
                    UniqueId = SelectedDevice.Id.ToString(),
                    Nickname = SelectedDevice.Name ?? "New Feeder",
                    IpAddress = ip
                });

                if (response.IsSuccessStatusCode)
                {
                    Status = "Setup Complete! Feeder is registered.";
                }
                else
                {
                    Status = "Provisioned, but Server Registration failed.";
                }
            }
            catch (Exception ex)
            {
                Status = $"Registration Error: {ex.Message}";
            }
        }
    }
}
