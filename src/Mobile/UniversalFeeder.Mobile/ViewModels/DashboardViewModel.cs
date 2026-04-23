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
using UniversalFeeder.Mobile.Models;
using UniversalFeeder.Mobile.Services;

namespace UniversalFeeder.Mobile.ViewModels
{
    public class DashboardViewModel : BindableObject
    {
        private readonly MqttService _mqttService;
        private readonly FeederStorageService _storageService;
        private readonly LogRepository _logRepository;
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

        private bool _isRefreshingLogs;
        public bool IsRefreshingLogs
        {
            get => _isRefreshingLogs;
            set { _isRefreshingLogs = value; OnPropertyChanged(); }
        }

        public DashboardViewModel(MqttService mqttService, FeederStorageService storageService, LogRepository logRepository)
        {
            _mqttService = mqttService;
            _storageService = storageService;
            _logRepository = logRepository;

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
                Status = success ? "Requested logs" : "Failed to request logs";
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

            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page != null)
            {
                var confirmed = await page.DisplayAlertAsync(
                    "Feed now?",
                    $"Dispense food from {SelectedFeeder.Nickname} for {FeedDurationSeconds} second(s)?",
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
                    ct => _mqttService.SendFeedCommandAsync(SelectedFeeder.UniqueId, FeedDurationSeconds * 1000),
                    progress: attempt => Status = $"Sending feed (attempt {attempt}/3)...");

                if (success)
                {
                    TryHaptic(HapticFeedbackType.LongPress);
                    Status = $"Feed command sent! ({FeedDurationSeconds}s)";
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
                    _ => _mqttService.SendChimeCommandAsync(SelectedFeeder.UniqueId),
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
