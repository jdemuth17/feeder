using System.Collections.ObjectModel;
using System.Windows.Input;
using Plugin.BLE.Abstractions.Contracts;
using UniversalFeeder.Mobile.Models;
using UniversalFeeder.Mobile.Services;

namespace UniversalFeeder.Mobile.ViewModels
{
    public class ProvisioningViewModel : BindableObject
    {
        private readonly BleService _bleService;
        private readonly FeederStorageService _storageService;
        private bool _isScanning;
        private DiscoveredFeeder? _selectedDevice;
        private string? _ssid;
        private string? _password;
        private string? _status;
        private bool _isBusy;

        public ObservableCollection<DiscoveredFeeder> Devices { get; } = new();

        public bool IsScanning
        {
            get => _isScanning;
            set { _isScanning = value; OnPropertyChanged(); }
        }

        public DiscoveredFeeder? SelectedDevice
        {
            get => _selectedDevice;
            set { _selectedDevice = value; OnPropertyChanged(); }
        }

        public string? Ssid
        {
            get => _ssid;
            set { _ssid = value; OnPropertyChanged(); }
        }

        public string? Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(); }
        }

        public string? Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        public ICommand ScanCommand { get; }
        public ICommand ProvisionCommand { get; }

        public ProvisioningViewModel(BleService bleService, FeederStorageService storageService)
        {
            _bleService = bleService;
            _storageService = storageService;
            ScanCommand = new Command(async () => await ScanAsync());
            ProvisionCommand = new Command(async () => await ProvisionAsync());
        }

        private async Task ScanAsync()
        {
            if (!_bleService.IsBluetoothOn)
            {
                Status = "Bluetooth is OFF. Please enable Bluetooth.";
                return;
            }

            IsScanning = true;
            IsBusy = true;
            Status = "Scanning for feeders...";
            Devices.Clear();
            SelectedDevice = null;

            try
            {
                var found = await _bleService.ScanForFeedersAsync();
                foreach (var device in found)
                {
                    Devices.Add(new DiscoveredFeeder(
                        device,
                        _bleService.GetDisplayName(device),
                        _bleService.GetDeviceIdentifier(device)));
                }

                if (found.Count == 1)
                {
                    SelectedDevice = Devices[0];
                    Status = "Found 1 feeder and selected it automatically.";
                    return;
                }

                Status = found.Any() ? $"Found {found.Count} feeder(s). Select one to continue." : "No feeders found. Make sure your feeder is in setup mode.";
            }
            catch (Exception ex)
            {
                Status = $"Scan error: {ex.Message}";
            }
            finally
            {
                IsScanning = false;
                IsBusy = false;
            }
        }

        private async Task ProvisionAsync()
        {
            if (SelectedDevice == null && Devices.Count == 1)
            {
                SelectedDevice = Devices[0];
            }

            if (string.IsNullOrEmpty(Ssid))
            {
                Status = "Enter Wi-Fi SSID.";
                return;
            }

            if (SelectedDevice == null)
            {
                Status = "Select a feeder first.";
                return;
            }

            IsBusy = true;
            Status = "Provisioning via BLE...";

            try
            {
                string? ip = await _bleService.ProvisionDeviceAsync(SelectedDevice.Device, Ssid, Password ?? string.Empty);

                if (string.IsNullOrEmpty(ip))
                {
                    Status = "Provisioning failed — no IP received. Check Wi-Fi credentials.";
                    return;
                }

                // Save feeder locally (no server needed)
                var feeder = new FeederDevice
                {
                    UniqueId = SelectedDevice.Device.Id.ToString(),
                    Nickname = SelectedDevice.DisplayName,
                    IpAddress = ip,
                    ProvisionedAt = DateTime.UtcNow
                };
                _storageService.AddFeeder(feeder);

                Status = $"Setup complete! {feeder.Nickname} (IP: {ip}) saved. Go to Home tab to control it.";
            }
            catch (Exception ex)
            {
                Status = $"Provisioning error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
