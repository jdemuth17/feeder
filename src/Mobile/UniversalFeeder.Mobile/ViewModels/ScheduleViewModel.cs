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
        // Cup value table shared with DashboardViewModel / FeedTypeViewModel
        public static readonly double[] CupValues = { 0.25, 0.5, 0.75, 1.0, 1.25, 1.5, 1.75, 2.0, 2.5, 3.0, 3.5, 4.0 };
        public static readonly string[] CupLabels = { "¼ cup", "½ cup", "¾ cup", "1 cup", "1¼ cups", "1½ cups", "1¾ cups", "2 cups", "2½ cups", "3 cups", "3½ cups", "4 cups" };

        private readonly MqttService _mqttService;
        private readonly FeedTypeService _feedTypeService;
        private string _feederId = string.Empty;

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public ObservableCollection<FeedingScheduleEntry> Entries { get; } = new();
        public ObservableCollection<FeedType> AvailableFeedTypes { get; } = new();

        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }

        public ScheduleViewModel(MqttService mqttService, FeedTypeService feedTypeService)
        {
            _mqttService = mqttService;
            _feedTypeService = feedTypeService;
            SaveCommand = new Command(async () => await SaveAsync());
            DeleteCommand = new Command<FeedingScheduleEntry>(entry => { if (entry != null) Entries.Remove(entry); });
        }

        public void SetFeederId(string id)
        {
            _feederId = id ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(_feederId))
                Task.Run(async () => await LoadSchedulesAsync());
        }

        public void ReloadFeedTypes()
        {
            AvailableFeedTypes.Clear();
            foreach (var ft in _feedTypeService.GetAll())
                AvailableFeedTypes.Add(ft);
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

        /// <summary>
        /// Adds a schedule entry. If <paramref name="feedType"/> is provided, duration is computed from
        /// cups × seconds-per-cup. Otherwise <paramref name="rawSeconds"/> is used directly.
        /// </summary>
        public bool AddEntry(TimeSpan time, FeedType? feedType, double cups, double rawSeconds,
                             int chimeLeadSeconds, int chimeCount, double chimeDurationSeconds,
                             bool enabled, out string? error)
        {
            error = null;
            double durationSeconds = feedType != null
                ? cups * feedType.SecondsPerCup
                : rawSeconds;

            if (time.TotalHours >= 24)
            {
                error = "Time must be between 00:00 and 23:59.";
                return false;
            }
            if (durationSeconds <= 0 || durationSeconds > 60)
            {
                error = "Computed duration must be between 1 and 60 seconds.";
                return false;
            }
            if (Entries.Any(e => e.Time == time))
            {
                error = "A schedule entry for that time already exists.";
                return false;
            }

            Entries.Add(new FeedingScheduleEntry
            {
                Time = time,
                DurationSeconds = durationSeconds,
                FeedTypeId = feedType?.Id,
                Cups = feedType != null ? cups : 0,
                ChimeLeadSeconds = Math.Max(0, chimeLeadSeconds),
                ChimeCount = Math.Clamp(chimeCount, 0, 10),
                ChimeDurationSeconds = Math.Clamp(chimeDurationSeconds, 0.1, 10.0),
                Enabled = enabled
            });
            return true;
        }

        private async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(_feederId)) return;

            var payload = Entries.Select(e => new
            {
                time = e.Time.ToString(@"hh\:mm"),
                duration_ms = (int)(e.DurationSeconds * 1000),
                chime_lead_ms = e.ChimeLeadSeconds * 1000,
                chime_count = e.ChimeCount,
                chime_duration_ms = (int)(e.ChimeDurationSeconds * 1000),
                enabled = e.Enabled
            }).ToArray();
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
