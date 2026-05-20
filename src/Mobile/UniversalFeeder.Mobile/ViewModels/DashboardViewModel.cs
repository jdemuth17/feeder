using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using UniversalFeeder.Mobile.Models;
using UniversalFeeder.Mobile.Services;

namespace UniversalFeeder.Mobile.ViewModels
{
    public class DashboardViewModel : BindableObject
    {
        private readonly MqttService _mqttService;
        private readonly FeederStorageService _storageService;
        private readonly LogRepository _logRepository;
        private readonly FeedTypeService _feedTypeService;
        private FeederDevice? _selectedFeeder;
        private string _status = "Not connected";
        private bool _isConnected;
        private bool _isBusy;
        private int _feedDurationSeconds = 5;
        private int _chimeCount = 3;
        private double _chimeDurationSeconds = 3.0;
        private int _chimeLeadSeconds = 0;
        public ObservableCollection<string> Logs { get; } = new();
        public ObservableCollection<FeederDevice> Feeders { get; } = new();
        public ObservableCollection<FeedType> AvailableFeedTypes { get; } = new();

        // ── Feed type + cups selection for manual feed ───────────────────────

        private FeedType? _selectedFeedType;
        public FeedType? SelectedFeedType
        {
            get => _selectedFeedType;
            set
            {
                _selectedFeedType = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelectedFeedType));
                OnPropertyChanged(nameof(ManualFeedSummary));
            }
        }
        public bool HasSelectedFeedType => _selectedFeedType != null;

        private int _selectedCupsIndex = 3; // default = 1 cup
        public int SelectedCupsIndex
        {
            get => _selectedCupsIndex;
            set
            {
                _selectedCupsIndex = Math.Clamp(value, 0, FeedTypeViewModel.CupValues.Length - 1);
                OnPropertyChanged();
                OnPropertyChanged(nameof(ManualFeedSummary));
            }
        }
        public double SelectedCups => FeedTypeViewModel.CupValues[_selectedCupsIndex];

        public string ManualFeedSummary
        {
            get
            {
                if (_selectedFeedType != null)
                {
                    double secs = SelectedCups * _selectedFeedType.SecondsPerCup;
                    return $"{FeedTypeViewModel.CupLabels[_selectedCupsIndex]} of {_selectedFeedType.Name} (≈ {secs:F1}s)";
                }
                return $"{_feedDurationSeconds} second(s)";
            }
        }

        public void ReloadFeedTypes()
        {
            AvailableFeedTypes.Clear();
            foreach (var ft in _feedTypeService.GetAll())
                AvailableFeedTypes.Add(ft);
        }

        public FeederDevice? SelectedFeeder
        {
            get => _selectedFeeder;
            set
            {
                if (_selectedFeeder == value) return;
                _selectedFeeder = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelectedFeeder));

                // Cancel any in-flight per-feeder tasks (subscribe/request/load) so
                // switching feeders quickly doesn't overlap requests/subscriptions.
                _selectionCts?.Cancel();
                _selectionCts = new CancellationTokenSource();
                var ct = _selectionCts.Token;

                if (IsConnected && _selectedFeeder != null)
                {
                    var feederId = _selectedFeeder.UniqueId;
                    _ = _mqttService.SubscribeToLogsAsync(feederId);
                    _ = _mqttService.RequestLogsAsync(feederId);
                    _ = LoadStoredLogsAsync(ct);
                }
            }
        }

        private CancellationTokenSource? _selectionCts;

        private async Task LoadStoredLogsAsync()
        {
            await LoadStoredLogsAsync(CancellationToken.None);
        }

        private async Task LoadStoredLogsAsync(CancellationToken ct)
        {
            try
            {
                if (SelectedFeeder == null) return;
                var feederId = SelectedFeeder.UniqueId;
                var items = await _logRepository.GetLogsForFeederAsync(feederId, 100);
                if (ct.IsCancellationRequested) return;
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (ct.IsCancellationRequested) return;
                    Logs.Clear();
                    foreach (var l in items)
                    {
                        Logs.Add($"{l.TimestampUtc:yyyy-MM-dd HH:mm:ss} {l.Status ?? l.RawJson}");
                    }
                });
            }
            catch { }
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

        public int ChimeCount
        {
            get => _chimeCount;
            set
            {
                int clamped = Math.Clamp((int)value, 0, 5);
                if (_chimeCount == clamped) return;
                _chimeCount = clamped;
                Preferences.Set("dash_chime_count", _chimeCount);
                OnPropertyChanged();
            }
        }

        public double ChimeDurationSeconds
        {
            get => _chimeDurationSeconds;
            set
            {
                double clamped = Math.Clamp(value, 0.2, 5.0);
                if (Math.Abs(_chimeDurationSeconds - clamped) < 0.01) return;
                _chimeDurationSeconds = clamped;
                Preferences.Set("dash_chime_duration", _chimeDurationSeconds);
                OnPropertyChanged();
            }
        }

        public int ChimeLeadSeconds
        {
            get => _chimeLeadSeconds;
            set
            {
                int clamped = Math.Clamp((int)value, 0, 30);
                if (_chimeLeadSeconds == clamped) return;
                _chimeLeadSeconds = clamped;
                Preferences.Set("dash_chime_lead", _chimeLeadSeconds);
                OnPropertyChanged();
            }
        }

        public ICommand ConnectCommand { get; }
        public ICommand FeedCommand { get; }
        public ICommand ChimeCommand { get; }
        public ICommand SendScheduleCommand { get; }
        public ICommand EditScheduleCommand { get; }
        public ICommand RequestLogsCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand RefreshLogsCommand { get; }
        public ICommand RemoveFeederCommand { get; }
        public ICommand RenameFeederCommand { get; }
        public ICommand EditFeedTypesCommand { get; }
        public ICommand ReconfigureWifiCommand { get; }

        private bool _isRefreshingLogs;
        public bool IsRefreshingLogs
        {
            get => _isRefreshingLogs;
            set { _isRefreshingLogs = value; OnPropertyChanged(); }
        }

        public DashboardViewModel(MqttService mqttService, FeederStorageService storageService, LogRepository logRepository, FeedTypeService feedTypeService)
        {
            _mqttService = mqttService;
            _storageService = storageService;
            _logRepository = logRepository;
            _feedTypeService = feedTypeService;

            ConnectCommand = new Command(async () => await ConnectAsync());
            FeedCommand = new Command(async () => await FeedAsync());
            ChimeCommand = new Command(async () => await ChimeAsync());
            SendScheduleCommand = new Command(async () => await SendScheduleAsync());
            EditScheduleCommand = new Command(async () => await EditScheduleAsync());
            RequestLogsCommand = new Command(async () => await RequestLogsAsync());
            RefreshCommand = new Command(LoadFeeders);
            RefreshLogsCommand = new Command(async () => await RefreshLogsAsync());
            RemoveFeederCommand = new Command<FeederDevice>(async f => await RemoveFeederAsync(f));
            RenameFeederCommand = new Command<FeederDevice>(async f => await RenameFeederAsync(f));
            EditFeedTypesCommand = new Command(async () => await EditFeedTypesAsync());
            ReconfigureWifiCommand = new Command(async () => await ReconfigureWifiAsync());

            _chimeCount = Preferences.Get("dash_chime_count", 3);
            _chimeDurationSeconds = Preferences.Get("dash_chime_duration", 3.0);
            _chimeLeadSeconds = Preferences.Get("dash_chime_lead", 0);

            _mqttService.ConnectionChanged += (s, connected) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    IsConnected = connected;
                    Status = connected ? "Connected to MQTT broker" : "Disconnected from MQTT";
                    if (connected && SelectedFeeder != null)
                    {
                        _ = _mqttService.SubscribeToLogsAsync(SelectedFeeder.UniqueId);
                        _ = _mqttService.RequestLogsAsync(SelectedFeeder.UniqueId);
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
                        Logs.Insert(0, FormatLogEntry(DateTime.Now, payload));
                    }
                });

                // Persist log asynchronously
                _ = SaveLogAsync(feederId, payload);
            };
        }

        private static string FormatLogEntry(DateTime localTime, string? payload)
        {
            var timeStr = localTime.ToString("MMM d, h:mm tt");
            if (string.IsNullOrWhiteSpace(payload))
                return $"{timeStr} — Event";
            try
            {
                using var doc = JsonDocument.Parse(payload);
                var root = doc.RootElement;

                // Schedule ack
                if (root.TryGetProperty("action", out var actionEl) && actionEl.GetString() == "ack_schedule")
                {
                    var ok = root.TryGetProperty("success", out var s) && s.GetBoolean();
                    return $"{timeStr} — Schedule {(ok ? "saved ✓" : "save failed ✗")}";
                }

                // Logs replay completion
                if (root.TryGetProperty("action", out var actEl2) && actEl2.GetString() == "logs_replay_complete")
                {
                    var n = root.TryGetProperty("count", out var c) ? c.GetInt32() : 0;
                    return $"{timeStr} — Log replay complete ({n} entries)";
                }

                // Feeding event
                bool success = root.TryGetProperty("success", out var sv) && sv.GetBoolean();
                bool manual = root.TryGetProperty("manual", out var mv) && mv.GetBoolean();
                string kind = manual ? "Manual feed" : "Scheduled feed";
                return $"{timeStr} — {kind} {(success ? "✓" : "✗")}";
            }
            catch
            {
                // Not JSON — show as-is but strip year from leading timestamp if present
                return $"{timeStr} — {payload}";
            }
        }

        private async Task SaveLogAsync(string feederId, string payload)
        {
            try
            {
                bool success = false;
                bool manual = false;
                try
                {
                    using var doc = JsonDocument.Parse(payload ?? string.Empty);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("success", out var sv)) success = sv.GetBoolean();
                    if (root.TryGetProperty("manual", out var mv)) manual = mv.GetBoolean();
                }
                catch { }

                var log = new FeedingLogDb
                {
                    FeederId = feederId ?? string.Empty,
                    TimestampUtc = DateTime.UtcNow,
                    RawJson = payload ?? string.Empty,
                    Success = success,
                    IsManual = manual,
                    Status = payload
                };

                await _logRepository.InsertLogAsync(log);
            }
            catch
            {
                // ignore persistence errors for now
            }
        }

        private async Task EditScheduleAsync()
        {
            if (SelectedFeeder == null) return;
            var id = SelectedFeeder.UniqueId;
            if (string.IsNullOrWhiteSpace(id)) return;
            var encoded = Uri.EscapeDataString(id);
            await Shell.Current.GoToAsync($"SchedulePage?feederId={encoded}");
        }

        private async Task EditFeedTypesAsync()
        {
            var feederId = SelectedFeeder?.UniqueId ?? string.Empty;
            var encoded = Uri.EscapeDataString(feederId);
            await Shell.Current.GoToAsync($"FeedTypePage?feederId={encoded}");
        }

        private async Task RequestLogsAsync()
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
            Status = $"Requesting logs from {SelectedFeeder.Nickname}...";
            try
            {
                var success = await _mqttService.RequestLogsAsync(SelectedFeeder.UniqueId);
                if (success)
                {
                    Status = "Waiting for logs...";
                    // Firmware replays stored entries as a burst on the logs topic.
                    // Give them a moment to land, then refresh from local DB.
                    await Task.Delay(1500);
                    await LoadStoredLogsAsync();
                    Status = $"Logs refreshed ({Logs.Count})";
                }
                else
                {
                    Status = "Failed to request logs";
                }
            }
            catch (Exception ex)
            {
                Status = $"Request logs error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ReconfigureWifiAsync()
        {
            if (SelectedFeeder == null)
            {
                Status = "Select a feeder first";
                return;
            }

            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page != null)
            {
                var confirmed = await page.DisplayAlertAsync(
                    "Change Wi-Fi?",
                    $"{SelectedFeeder.Nickname} will disconnect from the network and restart in setup mode. " +
                    "You will need to re-run the Setup flow to reconnect it.\n\n" +
                    "If the feeder is offline, hold the BOOT button on the device for 5 seconds instead.",
                    "Continue",
                    "Cancel");
                if (!confirmed) return;
            }

            if (!IsConnected)
            {
                if (page != null)
                {
                    await page.DisplayAlertAsync("Not connected", "Connect to MQTT first, or use the hardware BOOT button (hold 5s).", "OK");
                }
                Status = "Connect to MQTT first";
                return;
            }

            IsBusy = true;
            Status = $"Sending Wi-Fi reset to {SelectedFeeder.Nickname}...";
            try
            {
                var success = await _mqttService.SendWifiReconfigureAsync(SelectedFeeder.UniqueId);
                Status = success
                    ? "Wi-Fi reset sent. Feeder will restart in setup mode."
                    : "Failed to send Wi-Fi reset";
                if (success && page != null)
                {
                    await page.DisplayAlertAsync("Reset sent",
                        "The feeder will restart in BLE setup mode shortly. Open the Setup tab to reconnect it.",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                Status = $"Wi-Fi reset error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task RefreshLogsAsync()
        {
            IsRefreshingLogs = true;
            try
            {
                if (SelectedFeeder == null || !IsConnected)
                {
                    await LoadStoredLogsAsync();
                    return;
                }
                await _mqttService.RequestLogsAsync(SelectedFeeder.UniqueId);
                // Give the broker a moment to deliver queued logs before UI settles.
                await Task.Delay(600);
            }
            catch
            {
                // Swallow — pull-to-refresh should never crash the page.
            }
            finally
            {
                IsRefreshingLogs = false;
            }
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

            // Compute duration from feed type + cups, or fall back to raw slider
            int durationMs;
            string summary;
            if (_selectedFeedType != null)
            {
                double secs = SelectedCups * _selectedFeedType.SecondsPerCup;
                durationMs = Math.Max(1, (int)(secs * 1000));
                summary = ManualFeedSummary;
            }
            else
            {
                durationMs = FeedDurationSeconds * 1000;
                summary = $"{FeedDurationSeconds} second(s)";
            }

            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page != null)
            {
                var confirmed = await page.DisplayAlertAsync(
                    "Feed now?",
                    $"Dispense {summary} from {SelectedFeeder.Nickname}?",
                    "Feed",
                    "Cancel");
                if (!confirmed)
                {
                    Status = "Feed cancelled";
                    return;
                }
            }

            TryHaptic(HapticFeedbackType.Click);

            IsBusy = true;
            Status = $"Sending feed command to {SelectedFeeder.Nickname}...";
            try
            {
                var success = await PublishWithRetryAsync(
                    ct => _mqttService.SendFeedCommandAsync(SelectedFeeder.UniqueId, durationMs,
                                                            _chimeCount,
                                                            (int)(_chimeDurationSeconds * 1000),
                                                            _chimeLeadSeconds * 1000),
                    progress: attempt => Status = $"Sending feed (attempt {attempt}/3)...");

                if (success)
                {
                    TryHaptic(HapticFeedbackType.LongPress);
                    Status = $"Feed command sent! ({summary})";
                }
                else
                {
                    Status = "Failed to send feed command after 3 attempts";
                    if (page != null)
                    {
                        await page.DisplayAlertAsync("Feed failed", "Could not reach feeder. Check network and try again.", "OK");
                    }
                }
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

        private static void TryHaptic(HapticFeedbackType type)
        {
            try { HapticFeedback.Default.Perform(type); } catch { /* platform may not support */ }
        }

        private static async Task<bool> PublishWithRetryAsync(
            Func<CancellationToken, Task<bool>> action,
            Action<int>? progress = null,
            int maxAttempts = 3)
        {
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                progress?.Invoke(attempt);
                try
                {
                    if (await action(CancellationToken.None))
                    {
                        return true;
                    }
                }
                catch
                {
                    // swallow and retry; last failure surfaces as "false"
                }

                if (attempt < maxAttempts)
                {
                    // 500ms, 1s, 2s backoff
                    await Task.Delay(TimeSpan.FromMilliseconds(500 * (1 << (attempt - 1))));
                }
            }
            return false;
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

            TryHaptic(HapticFeedbackType.Click);

            IsBusy = true;
            Status = $"Sending chime to {SelectedFeeder.Nickname}...";
            try
            {
                var success = await PublishWithRetryAsync(
                    _ => _mqttService.SendChimeCommandAsync(SelectedFeeder.UniqueId, 1.0f,
                                                            _chimeCount,
                                                            (int)(_chimeDurationSeconds * 1000)),
                    progress: attempt => Status = $"Sending chime (attempt {attempt}/3)...");
                Status = success ? "Chime sent!" : "Failed to send chime after 3 attempts";
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

        private Task SendScheduleAsync()
        {
            // Left in place for backward compatibility — the dashboard navigates to
            // the Schedule editor instead of sending a hardcoded sample schedule.
            return EditScheduleAsync();
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

        private async Task RemoveFeederAsync(FeederDevice? feeder)
        {
            if (feeder == null) return;
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page != null)
            {
                var confirmed = await page.DisplayAlertAsync(
                    "Remove feeder?",
                    $"Remove {feeder.Nickname}? You will need to re-provision it to use it again.",
                    "Remove",
                    "Cancel");
                if (!confirmed) return;
            }

            RemoveFeeder(feeder);
            TryHaptic(HapticFeedbackType.Click);
        }

        private async Task RenameFeederAsync(FeederDevice? feeder)
        {
            if (feeder == null) return;
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page == null) return;
            var result = await page.DisplayPromptAsync(
                "Rename Feeder",
                "Enter a new name:",
                initialValue: feeder.Nickname,
                maxLength: 32,
                keyboard: Keyboard.Text);
            if (result == null) return; // cancelled
            var trimmed = result.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) return;
            feeder.Nickname = trimmed;
            _storageService.AddFeeder(feeder); // updates existing entry
            LoadFeeders();
        }
    }
}
