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
        public string Nickname { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        
        public int? FeedTypeId { get; set; }
        public FeedType? FeedType { get; set; }
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
}
