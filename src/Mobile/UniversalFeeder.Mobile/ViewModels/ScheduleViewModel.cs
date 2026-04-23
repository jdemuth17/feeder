using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using UniversalFeeder.Mobile.Models;
using UniversalFeeder.Mobile.Services;

namespace UniversalFeeder.Mobile.ViewModels
{
    public class ScheduleViewModel : BindableObject
    {
        private readonly MqttService _mqttService;
        private string _feederId = string.Empty;

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public ObservableCollection<FeedingScheduleEntry> Entries { get; } = new();

        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }

        public ScheduleViewModel(MqttService mqttService)
        {
            _mqttService = mqttService;
            SaveCommand = new Command(async () => await SaveAsync());
            DeleteCommand = new Command<FeedingScheduleEntry>(entry => { if (entry != null) Entries.Remove(entry); });
        }

        public void SetFeederId(string id)
        {
            _feederId = id ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(_feederId))
                Task.Run(async () => await LoadSchedulesAsync());
        }

        public async Task LoadSchedulesAsync()
        {
            if (string.IsNullOrWhiteSpace(_feederId)) return;
            IsLoading = true;
            try
            {
                var entries = await _mqttService.RequestScheduleAsync(_feederId);
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Entries.Clear();
                    foreach (var e in entries)
                        Entries.Add(e);
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        public bool AddEntry(TimeSpan time, double durationSeconds, bool enabled, out string? error)
        {
            error = null;
            if (time.TotalMinutes < 0 || time.TotalHours >= 24)
            {
                error = "Time must be between 00:00 and 23:59.";
                return false;
            }
            if (durationSeconds <= 0 || durationSeconds > 60)
            {
                error = "Duration must be between 1 and 60 seconds.";
                return false;
            }
            if (Entries.Any(e => e.Time == time))
            {
                error = "A schedule entry for that time already exists.";
                return false;
            }
            Entries.Add(new FeedingScheduleEntry { Time = time, DurationSeconds = durationSeconds, Enabled = enabled });
            return true;
        }

        public void AddEntry(TimeSpan time, double durationSeconds, bool enabled)
        {
            AddEntry(time, durationSeconds, enabled, out _);
        }

        private async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(_feederId)) return;

            var payload = Entries.Select(e => new { time = e.Time.ToString(@"hh\:mm"), duration_ms = (int)(e.DurationSeconds * 1000), enabled = e.Enabled }).ToArray();
            try
            {
                var ok = await _mqttService.SendScheduleAsync(_feederId, payload);
                if (!ok)
                {
                    await ShowErrorAsync("Could not send schedule. Check that the feeder is online.");
                    return;
                }
                await Shell.Current.GoToAsync("..", animate: false);
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Failed to save schedule: {ex.Message}");
            }
        }

        private static async Task ShowErrorAsync(string message)
        {
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page != null)
            {
                await page.DisplayAlertAsync("Schedule error", message, "OK");
            }
        }
    }
}
