using System.Collections.ObjectModel;
using System.Threading;
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
        private CancellationTokenSource? _provisionCts;

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
            set { _isBusy = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanCancel)); }
        }

        public bool CanCancel => _isBusy && _provisionCts is { IsCancellationRequested: false };

        public ICommand ScanCommand { get; }
        public ICommand ProvisionCommand { get; }
        public ICommand CancelProvisionCommand { get; }

        public ProvisioningViewModel(BleService bleService, FeederStorageService storageService)
        {
            _bleService = bleService;
            _storageService = storageService;
            ScanCommand = new Command(async () => await ScanAsync());
            ProvisionCommand = new Command(async () => await ProvisionAsync());
            CancelProvisionCommand = new Command(() =>
            {
                _provisionCts?.Cancel();
                Status = "Cancelling…";
            });
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

            _provisionCts?.Dispose();
            _provisionCts = new CancellationTokenSource();
            var ct = _provisionCts.Token;

            IsBusy = true;
            Status = "Starting provisioning…";

            var progress = new Progress<string>(msg => MainThread.BeginInvokeOnMainThread(() => Status = msg));

            try
            {
                string? result = await _bleService.ProvisionDeviceAsync(
                    SelectedDevice.Device,
                    Ssid,
                    Password ?? string.Empty,
                    progress,
                    ct);

                if (string.IsNullOrEmpty(result))
                {
                    Status = "Provisioning failed — no IP received. Check Wi-Fi credentials.";
                    return;
                }

                // Parse piped result "IP|DeviceId"
                var parts = result.Split('|', 2);
                string ip = parts[0];
                string deviceId = parts.Length > 1 ? parts[1] : SelectedDevice.Device.Id.ToString();

                // Save feeder locally
                var feeder = new FeederDevice
                {
                    UniqueId = deviceId, // Use the MAC address from ESP32
                    Nickname = SelectedDevice.DisplayName,
                    IpAddress = ip,
                    ProvisionedAt = DateTime.UtcNow
                };
                _storageService.AddFeeder(feeder);

                Status = string.IsNullOrWhiteSpace(ip) || ip == "0.0.0.0"
                    ? $"Saved {feeder.Nickname}, but no IP yet. Open Home after the feeder joins Wi-Fi."
                    : $"Setup complete! {feeder.Nickname} is at {ip}. Open Home to control it.";
            }
            catch (OperationCanceledException)
            {
                Status = "Provisioning cancelled.";
            }
            catch (Exception ex)
            {
                Status = $"Provisioning error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
                _provisionCts?.Dispose();
                _provisionCts = null;
                OnPropertyChanged(nameof(CanCancel));
            }
        }
    }
}
