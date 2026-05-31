using System;
using System.Collections;
using UniversalFeeder.Shared;

namespace UniversalFeeder.Firmware
{
    /// <summary>
    /// Simple data class representing one feeding schedule entry.
    /// Stored in RAM; survives as long as the device is powered.
    /// </summary>
    public class FeedScheduleEntry
    {
        public string Id;
        public int Hour;            // 0-23 (local time, offset applied at check-time)
        public int Minute;          // 0-59
        public int DurationSeconds; // motor run time
        public bool PlayChime;
        public bool IsEnabled;
        public bool[] Days;         // [0]=Mon … [6]=Sun

        /// <summary>UTC timestamp of last execution (prevents double-fire).</summary>
        public DateTime LastFired;

        public FeedScheduleEntry()
        {
            Days = new bool[7];
            for (int i = 0; i < 7; i++) Days[i] = true;
            LastFired = DateTime.MinValue;
        }
    }

    /// <summary>
    /// In-memory store for feeding schedules on the ESP32.
    /// The mobile app pushes schedules via MQTT; the device runs them autonomously.
    /// </summary>
    public class ScheduleStorageService
    {
        private readonly ArrayList _schedules = new ArrayList();

        /// <summary>
        /// UTC offset in minutes supplied by the mobile app (e.g. EST = -300).
        /// Used to derive local wall-clock time from DateTime.UtcNow.
        /// </summary>
        public int UtcOffsetMinutes { get; set; }

        // ── CRUD ──────────────────────────────────────────────────

        public void AddOrUpdate(FeedScheduleEntry entry)
        {
            for (int i = _schedules.Count - 1; i >= 0; i--)
            {
                if (((FeedScheduleEntry)_schedules[i]).Id == entry.Id)
                {
                    _schedules.RemoveAt(i);
                    break;
                }
            }
            _schedules.Add(entry);
            Console.WriteLine($"Schedule saved: {entry.Id} at {entry.Hour}:{entry.Minute:D2}");
        }

        public bool Remove(string scheduleId)
        {
            for (int i = _schedules.Count - 1; i >= 0; i--)
            {
                if (((FeedScheduleEntry)_schedules[i]).Id == scheduleId)
                {
                    _schedules.RemoveAt(i);
                    Console.WriteLine($"Schedule deleted: {scheduleId}");
                    return true;
                }
            }
            return false;
        }

        public ArrayList GetAll() => _schedules;
        public int Count => _schedules.Count;

        // ── Helpers ───────────────────────────────────────────────

        /// <summary>Convert bool[7] → "1111100" (Mon–Sun).</summary>
        public static string DaysToString(bool[] days)
        {
            string result = "";
            for (int i = 0; i < 7; i++)
                result += days[i] ? "1" : "0";
            return result;
        }

        /// <summary>Convert "1111100" → bool[7].</summary>
        public static bool[] StringToDays(string daysStr)
        {
            bool[] days = new bool[7];
            if (daysStr != null && daysStr.Length == 7)
            {
                for (int i = 0; i < 7; i++)
                    days[i] = daysStr[i] == '1';
            }
            return days;
        }

        /// <summary>
        /// Build a JSON string listing every stored schedule.
        /// Used to reply on the status topic so the mobile app can sync.
        /// </summary>
        public string ToScheduleListJson()
        {
            string json = "{\"" + MqttCommands.KeyAction + "\":\"" + MqttCommands.ActionSchedulesList + "\",\"schedules\":[";

            for (int i = 0; i < _schedules.Count; i++)
            {
                var s = (FeedScheduleEntry)_schedules[i];
                if (i > 0) json += ",";
                json += "{"
                    + "\"" + MqttCommands.KeyScheduleId + "\":\"" + s.Id + "\""
                    + ",\"" + MqttCommands.KeyTimeHour + "\":" + s.Hour
                    + ",\"" + MqttCommands.KeyTimeMinute + "\":" + s.Minute
                    + ",\"" + MqttCommands.KeyDurationSec + "\":" + s.DurationSeconds
                    + ",\"" + MqttCommands.KeyPlayChime + "\":" + (s.PlayChime ? "1" : "0")
                    + ",\"" + MqttCommands.KeyEnabled + "\":" + (s.IsEnabled ? "1" : "0")
                    + ",\"" + MqttCommands.KeyDays + "\":\"" + DaysToString(s.Days) + "\""
                    + "}";
            }

            json += "]}";
            return json;
        }
    }
}
