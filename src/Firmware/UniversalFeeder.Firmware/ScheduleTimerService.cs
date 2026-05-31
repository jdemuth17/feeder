using System;
using System.Threading;

namespace UniversalFeeder.Firmware
{
    /// <summary>
    /// Runs on the ESP32. Checks stored schedules every 30 seconds and
    /// fires the feeding sequence when a schedule is due.
    /// Uses the UTC offset from <see cref="ScheduleStorageService"/> to
    /// derive local wall-clock time (schedules are stored in local time).
    /// </summary>
    public class ScheduleTimerService : IDisposable
    {
        private readonly ScheduleStorageService _storage;
        private readonly IFeedingSequenceService _feedingSequence;
        private readonly IBuzzerService _buzzerService;
        private Timer _timer;

        public ScheduleTimerService(
            ScheduleStorageService storage,
            IFeedingSequenceService feedingSequence,
            IBuzzerService buzzerService)
        {
            _storage = storage;
            _feedingSequence = feedingSequence;
            _buzzerService = buzzerService;
        }

        public void Start()
        {
            // First check after 15 s, then every 30 s
            _timer = new Timer(CheckSchedules, null,
                TimeSpan.FromSeconds(15),
                TimeSpan.FromSeconds(30));
            Console.WriteLine("ScheduleTimer started (30 s interval).");
        }

        private void CheckSchedules(object state)
        {
            try
            {
                var utcNow = DateTime.UtcNow;

                // Guard: if NTP has not synced yet the year will be ~2000
                if (utcNow.Year < 2024) return;

                // Apply the UTC offset the mobile told us about
                var localNow = utcNow.AddMinutes(_storage.UtcOffsetMinutes);

                var schedules = _storage.GetAll();
                for (int i = 0; i < schedules.Count; i++)
                {
                    var s = (FeedScheduleEntry)schedules[i];
                    if (!s.IsEnabled) continue;

                    // ── Day-of-week check ──
                    int dayIndex = MapDayOfWeek(localNow.DayOfWeek);
                    if (!s.Days[dayIndex]) continue;

                    // ── Time check (must match exact hour:minute) ──
                    if (localNow.Hour != s.Hour || localNow.Minute != s.Minute) continue;

                    // ── Already fired this minute? ──
                    var lastLocal = s.LastFired.AddMinutes(_storage.UtcOffsetMinutes);
                    if (lastLocal.Year == localNow.Year
                        && lastLocal.Month == localNow.Month
                        && lastLocal.Day == localNow.Day
                        && lastLocal.Hour == localNow.Hour
                        && lastLocal.Minute == localNow.Minute)
                    {
                        continue;
                    }

                    // ── Fire! ──
                    s.LastFired = utcNow;
                    Console.WriteLine($"[Schedule] Firing {s.Id} – {s.DurationSeconds}s feed" +
                                      (s.PlayChime ? " + chime" : ""));

                    // Optional pre-feed chime (separate from the chime inside Execute)
                    if (s.PlayChime)
                    {
                        _buzzerService.Play(1.0f, 1000);
                        Thread.Sleep(1500);
                    }

                    _feedingSequence.Execute(s.DurationSeconds * 1000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ScheduleTimer error: {ex.Message}");
            }
        }

        /// <summary>
        /// Maps .NET DayOfWeek (Sunday=0 … Saturday=6)
        /// to our days-array index (Mon=0 … Sun=6).
        /// </summary>
        private static int MapDayOfWeek(DayOfWeek dow)
        {
            switch (dow)
            {
                case DayOfWeek.Monday:    return 0;
                case DayOfWeek.Tuesday:   return 1;
                case DayOfWeek.Wednesday: return 2;
                case DayOfWeek.Thursday:  return 3;
                case DayOfWeek.Friday:    return 4;
                case DayOfWeek.Saturday:  return 5;
                case DayOfWeek.Sunday:    return 6;
                default:                  return 0;
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
