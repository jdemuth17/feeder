using System.Text.Json;
using UniversalFeeder.Mobile.Models;

namespace UniversalFeeder.Mobile.Services
{
    public class ScheduleStorageService
    {
        private const string StorageKey = "feeding_schedules";
        private const string LastRunKey = "schedule_last_run";
        private List<FeedingSchedule>? _cache;

        public List<FeedingSchedule> GetSchedules()
        {
            if (_cache != null) return _cache;

            var json = Preferences.Get(StorageKey, string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                _cache = new List<FeedingSchedule>();
                return _cache;
            }

            try
            {
                _cache = JsonSerializer.Deserialize<List<FeedingSchedule>>(json) ?? new List<FeedingSchedule>();
            }
            catch
            {
                _cache = new List<FeedingSchedule>();
            }

            return _cache;
        }

        public void AddOrUpdate(FeedingSchedule schedule)
        {
            var schedules = GetSchedules();
            schedules.RemoveAll(s => s.Id == schedule.Id);
            schedules.Add(schedule);
            Save(schedules);
        }

        public void Remove(string scheduleId)
        {
            var schedules = GetSchedules();
            schedules.RemoveAll(s => s.Id == scheduleId);
            Save(schedules);
        }

        public void SetLastRun(string scheduleId, DateTime utcNow)
        {
            Preferences.Set($"{LastRunKey}_{scheduleId}", utcNow.ToString("O"));
        }

        public DateTime? GetLastRun(string scheduleId)
        {
            var val = Preferences.Get($"{LastRunKey}_{scheduleId}", string.Empty);
            if (string.IsNullOrEmpty(val)) return null;
            return DateTime.TryParse(val, out var dt) ? dt : null;
        }

        private void Save(List<FeedingSchedule> schedules)
        {
            _cache = schedules;
            var json = JsonSerializer.Serialize(schedules);
            Preferences.Set(StorageKey, json);
        }
    }
}
