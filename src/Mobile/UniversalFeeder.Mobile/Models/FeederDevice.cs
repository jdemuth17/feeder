namespace UniversalFeeder.Mobile.Models
{
    public class FeederDevice
    {
        public string UniqueId { get; set; } = string.Empty;
        public string Nickname { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public DateTime ProvisionedAt { get; set; } = DateTime.UtcNow;
    }
}
