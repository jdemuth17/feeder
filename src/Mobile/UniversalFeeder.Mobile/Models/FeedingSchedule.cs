namespace UniversalFeeder.Mobile.Models
{
    public class FeedingSchedule
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string FeederId { get; set; } = string.Empty;
        public string FeederNickname { get; set; } = string.Empty;
        public TimeSpan TimeOfDay { get; set; } = new(8, 0, 0);
        public int DurationSeconds { get; set; } = 5;
        public bool IsEnabled { get; set; } = true;
        public bool PlayChime { get; set; } = true;

        // Days of week
        public bool Monday { get; set; } = true;
        public bool Tuesday { get; set; } = true;
        public bool Wednesday { get; set; } = true;
        public bool Thursday { get; set; } = true;
        public bool Friday { get; set; } = true;
        public bool Saturday { get; set; } = true;
        public bool Sunday { get; set; } = true;

        public bool IsActiveOnDay(DayOfWeek day) => day switch
        {
            DayOfWeek.Monday => Monday,
            DayOfWeek.Tuesday => Tuesday,
            DayOfWeek.Wednesday => Wednesday,
            DayOfWeek.Thursday => Thursday,
            DayOfWeek.Friday => Friday,
            DayOfWeek.Saturday => Saturday,
            DayOfWeek.Sunday => Sunday,
            _ => false
        };

        public string DaysSummary
        {
            get
            {
                if (Monday && Tuesday && Wednesday && Thursday && Friday && Saturday && Sunday)
                    return "Every day";
                if (Monday && Tuesday && Wednesday && Thursday && Friday && !Saturday && !Sunday)
                    return "Weekdays";
                if (!Monday && !Tuesday && !Wednesday && !Thursday && !Friday && Saturday && Sunday)
                    return "Weekends";

                var days = new List<string>();
                if (Monday) days.Add("Mon");
                if (Tuesday) days.Add("Tue");
                if (Wednesday) days.Add("Wed");
                if (Thursday) days.Add("Thu");
                if (Friday) days.Add("Fri");
                if (Saturday) days.Add("Sat");
                if (Sunday) days.Add("Sun");
                return string.Join(", ", days);
            }
        }

        public string TimeSummary => DateTime.Today.Add(TimeOfDay).ToString("h:mm tt");
    }
}
