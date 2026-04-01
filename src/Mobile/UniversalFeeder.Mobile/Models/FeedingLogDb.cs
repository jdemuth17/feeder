using System;
using SQLite;

namespace UniversalFeeder.Mobile.Models
{
    public class FeedingLogDb
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string FeederId { get; set; }

        public DateTime TimestampUtc { get; set; }

        public bool Success { get; set; }

        public string Status { get; set; }

        public bool IsManual { get; set; }

        public string RawJson { get; set; }
    }
}
