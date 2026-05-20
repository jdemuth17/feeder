namespace UniversalFeeder.Mobile.Models
{
    public class FeedType
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = string.Empty;

        /// <summary>Seconds of motor run required to dispense exactly one cup.</summary>
        public double SecondsPerCup { get; set; } = 5.0;
    }
}
