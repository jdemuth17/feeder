using Plugin.BLE.Abstractions.Contracts;

namespace UniversalFeeder.Mobile.Models
{
    public sealed class DiscoveredFeeder
    {
        public DiscoveredFeeder(IDevice device, string displayName, string secondaryText)
        {
            Device = device;
            DisplayName = displayName;
            SecondaryText = secondaryText;
        }

        public IDevice Device { get; }
        public string DisplayName { get; }
        public string SecondaryText { get; }
    }
}