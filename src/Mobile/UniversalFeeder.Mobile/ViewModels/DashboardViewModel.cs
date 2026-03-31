using System.Collections.ObjectModel;
using System.Windows.Input;
using UniversalFeeder.Mobile.Models;
using UniversalFeeder.Mobile.Services;

namespace UniversalFeeder.Mobile.ViewModels
{
    public class DashboardViewModel : BindableObject
    {
        private readonly MqttService _mqttService;
        private readonly FeederStorageService _storageService;
        private FeederDevice? _selectedFeeder;
        private string _status = "Not connected";
        private bool _isConnected;
        private bool _isBusy;
        private int _feedDurationSeconds = 5;
        public ObservableCollection<string> Logs { get; } = new();

        public ObservableCollection<FeederDevice> Feeders { get; } = new();

        public FeederDevice? SelectedFeeder
        {
            get => _selectedFeeder;
            set
            {
                _selectedFeeder = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelectedFeeder));
                if (IsConnected && _selectedFeeder != null)
                {
                    _ = _mqttService.SubscribeToLogsAsync(_selectedFeeder.UniqueId);
                }
            }
        }

        public bool HasSelectedFeeder => _selectedFeeder != null;

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public bool IsConnected
        {
            get => _isConnected;
            set { _isConnected = value; OnPropertyChanged(); OnPropertyChanged(nameof(ConnectionStatusText)); OnPropertyChanged(nameof(ConnectionStatusColor)); OnPropertyChanged(nameof(ConnectButtonText)); }
        }

        public string ConnectionStatusText => IsConnected ? "Connected to MQTT" : "Disconnected";
        public Color ConnectionStatusColor => IsConnected ? Colors.Green : Colors.Red;
        public string ConnectButtonText => IsConnected ? "Disconnect" : "Connect";

        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        public int FeedDurationSeconds
        {
            get => _feedDurationSeconds;
            set { _feedDurationSeconds = Math.Max(1, Math.Min(30, value)); OnPropertyChanged(); }
        }

        public ICommand ConnectCommand { get; }
        public ICommand FeedCommand { get; }
        public ICommand ChimeCommand { get; }
        public ICommand SendScheduleCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand RemoveFeederCommand { get; }

        public DashboardViewModel(MqttService mqttService, FeederStorageService storageService)
        {
            _mqttService = mqttService;
            _storageService = storageService;

            ConnectCommand = new Command(async () => await ConnectAsync());
            FeedCommand = new Command(async () => await FeedAsync());
            ChimeCommand = new Command(async () => await ChimeAsync());
            SendScheduleCommand = new Command(async () => await SendScheduleAsync());
            RefreshCommand = new Command(LoadFeeders);
            RemoveFeederCommand = new Command<FeederDevice>(RemoveFeeder);

            _mqttService.ConnectionChanged += (s, connected) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    IsConnected = connected;
                    Status = connected ? "Connected to MQTT broker" : "Disconnected from MQTT";
                    if (connected && SelectedFeeder != null)
                    {
                        _ = _mqttService.SubscribeToLogsAsync(SelectedFeeder.UniqueId);
                    }
                });
            };

            _storageService.FeedersChanged += (s, e) =>
            {
                MainThread.BeginInvokeOnMainThread(LoadFeeders);
            };

            LoadFeeders();

            _mqttService.LogMessageReceived += (s, tuple) =>
            {
                var (feederId, payload) = tuple;
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (SelectedFeeder == null || SelectedFeeder.UniqueId == feederId)
                    {
                        Logs.Insert(0, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {payload}");
                    }
                });
            };
        }

        public void LoadFeeders()
        {
            Feeders.Clear();
            foreach (var f in _storageService.GetFeeders())
            {
                Feeders.Add(f);
            }

            if (Feeders.Count > 0 && SelectedFeeder == null)
            {
                SelectedFeeder = Feeders[0];
            }
        }

        private async Task ConnectAsync()
        {
            if (IsConnected)
            {
                await _mqttService.DisconnectAsync();
                return;
            }

            IsBusy = true;
            Status = "Connecting to MQTT...";
            try
            {
                await _mqttService.ConnectAsync();
                IsConnected = _mqttService.IsConnected;
                Status = IsConnected ? "Connected!" : "Connection failed";
            }
            catch (Exception ex)
            {
                var fullError = ex.InnerException != null
                    ? $"{ex.Message} → {ex.InnerException.Message}"
                    : ex.Message;
                Status = $"Error: {fullError}";
                if (Application.Current?.Windows.FirstOrDefault()?.Page is Page page)
                    await page.DisplayAlertAsync("MQTT Error", fullError, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task FeedAsync()
        {
            if (SelectedFeeder == null)
            {
                Status = "Select a feeder first";
                return;
            }

            if (!IsConnected)
            {
                Status = "Connect to MQTT first";
                return;
            }

            IsBusy = true;
            Status = $"Sending feed command to {SelectedFeeder.Nickname}...";
            try
            {
                var success = await _mqttService.SendFeedCommandAsync(
                    SelectedFeeder.UniqueId,
                    FeedDurationSeconds * 1000);

                Status = success
                    ? $"Feed command sent! ({FeedDurationSeconds}s)"
                    : "Failed to send feed command";
            }
            catch (Exception ex)
            {
                Status = $"Feed error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ChimeAsync()
        {
            if (SelectedFeeder == null)
            {
                Status = "Select a feeder first";
                return;
            }

            if (!IsConnected)
            {
                Status = "Connect to MQTT first";
                return;
            }

            IsBusy = true;
            Status = $"Sending chime to {SelectedFeeder.Nickname}...";
            try
            {
                var success = await _mqttService.SendChimeCommandAsync(SelectedFeeder.UniqueId);
                Status = success ? "Chime sent!" : "Failed to send chime";
            }
            catch (Exception ex)
            {
                Status = $"Chime error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SendScheduleAsync()
        {
            if (SelectedFeeder == null)
            {
                Status = "Select a feeder first";
                return;
            }

            if (!IsConnected)
            {
                Status = "Connect to MQTT first";
                return;
            }

            // Sample schedule: morning and evening
            var schedule = new[]
            {
                new { time = "08:00", amount = 50.0, enabled = true },
                new { time = "18:00", amount = 50.0, enabled = true }
            };

            IsBusy = true;
            Status = $"Sending schedule to {SelectedFeeder.Nickname}...";
            try
            {
                var success = await _mqttService.SendScheduleAsync(SelectedFeeder.UniqueId, schedule);
                Status = success ? "Schedule sent!" : "Failed to send schedule";
            }
            catch (Exception ex)
            {
                Status = $"Schedule error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void RemoveFeeder(FeederDevice? feeder)
        {
            if (feeder == null) return;
            _storageService.RemoveFeeder(feeder.UniqueId);
            Feeders.Remove(feeder);
            if (SelectedFeeder == feeder)
                SelectedFeeder = Feeders.FirstOrDefault();
            Status = $"Removed {feeder.Nickname}";
        }
    }
}
