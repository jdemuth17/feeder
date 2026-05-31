using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using UniversalFeeder.Mobile.Services;

namespace UniversalFeeder.Mobile.ViewModels
{
    public class LogsViewModel : BindableObject, IQueryAttributable
    {
        private readonly MqttService _mqttService;
        private readonly LogRepository _logRepository;

        public ObservableCollection<string> Logs { get; } = new();

        private string _feederId = "";
        private string _feederName = "";
        private string _status = "";
        private bool _isRefreshing;

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set { _isRefreshing = value; OnPropertyChanged(); }
        }

        public ICommand RefreshCommand { get; }
        public ICommand RequestLogsCommand { get; }

        public LogsViewModel(MqttService mqttService, LogRepository logRepository)
        {
            _mqttService = mqttService;
            _logRepository = logRepository;

            RefreshCommand = new Command(async () => await RefreshAsync());
            RequestLogsCommand = new Command(async () => await RequestLogsAsync());
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("feederId", out var id))
                _feederId = id?.ToString() ?? "";
            if (query.TryGetValue("feederName", out var name))
                _feederName = name?.ToString() ?? _feederId;
        }

        public void Subscribe()
        {
            _mqttService.LogMessageReceived += OnLogMessageReceived;
            _ = LoadStoredLogsAsync();
        }

        public void Unsubscribe()
        {
            _mqttService.LogMessageReceived -= OnLogMessageReceived;
        }

        private void OnLogMessageReceived(object? sender, (string feederId, string payload) tuple)
        {
            var (feederId, payload) = tuple;
            if (!string.IsNullOrEmpty(_feederId) && feederId != _feederId) return;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Logs.Insert(0, FormatLogEntry(DateTime.Now, payload));
                Status = $"Showing {Logs.Count} log entries";
            });
        }

        private async Task LoadStoredLogsAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_feederId)) return;
                var items = await _logRepository.GetLogsForFeederAsync(_feederId, 100);
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Logs.Clear();
                    foreach (var l in items)
                        Logs.Add(FormatLogEntry(l.TimestampUtc.ToLocalTime(), l.RawJson));
                    Status = Logs.Count > 0
                        ? $"Showing {Logs.Count} log entries"
                        : "No stored logs. Pull down to request from feeder.";
                });
            }
            catch { }
        }

        private async Task RefreshAsync()
        {
            IsRefreshing = true;
            try
            {
                if (!string.IsNullOrEmpty(_feederId))
                {
                    await _mqttService.RequestLogsAsync(_feederId);
                    await Task.Delay(1500);
                    await LoadStoredLogsAsync();
                }
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private async Task RequestLogsAsync()
        {
            if (string.IsNullOrEmpty(_feederId)) return;
            Status = "Requesting logs from feeder…";
            await _mqttService.RequestLogsAsync(_feederId);
            await Task.Delay(1500);
            await LoadStoredLogsAsync();
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

                if (root.TryGetProperty("action", out var actionEl) && actionEl.GetString() == "ack_schedule")
                {
                    var ok = root.TryGetProperty("success", out var s) && s.GetBoolean();
                    return $"{timeStr} — Schedule {(ok ? "saved ✓" : "save failed ✗")}";
                }

                if (root.TryGetProperty("action", out var actEl2) && actEl2.GetString() == "logs_replay_complete")
                {
                    var n = root.TryGetProperty("count", out var c) ? c.GetInt32() : 0;
                    return $"{timeStr} — Log replay complete ({n} entries)";
                }

                bool success = root.TryGetProperty("success", out var sv) && sv.GetBoolean();
                bool manual = root.TryGetProperty("manual", out var mv) && mv.GetBoolean();
                string kind = manual ? "Manual feed" : "Scheduled feed";
                return $"{timeStr} — {kind} {(success ? "✓" : "✗")}";
            }
            catch
            {
                return $"{timeStr} — {payload}";
            }
        }
    }
}
