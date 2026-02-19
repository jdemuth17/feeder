using System;
using System.Collections.Generic;

namespace UniversalFeeder.Server.Models
{
    public class FeedType
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double GramsPerSecond { get; set; } = 10.0;
    }

    public class Feeder
    {
        public int Id { get; set; }
        public string UniqueId { get; set; } = string.Empty;
        public string Nickname { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        
        public int? FeedTypeId { get; set; }
        public FeedType? FeedType { get; set; }

        public ICollection<FeedingSchedule> Schedules { get; set; } = new List<FeedingSchedule>();
    }

    public class FeedingSchedule
    {
        public int Id { get; set; }
        public int FeederId { get; set; }
        public Feeder? Feeder { get; set; }
        public TimeSpan TimeOfDay { get; set; }
        public double AmountInGrams { get; set; }
        public bool IsEnabled { get; set; } = true;
    }

    public class FeedingLog
    {
        public int Id { get; set; }
        public int FeederId { get; set; }
        public DateTime Timestamp { get; set; }
        public bool Success { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsManualOverride { get; set; }
    }

    public class SystemSetting
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
