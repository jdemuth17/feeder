using System.Text.Json;
using UniversalFeeder.Mobile.Models;

namespace UniversalFeeder.Mobile.Services
{
    public class FeederStorageService
    {
        private const string StorageKey = "registered_feeders";
        private List<FeederDevice>? _cache;

        public List<FeederDevice> GetFeeders()
        {
            if (_cache != null) return _cache;

            var json = Preferences.Get(StorageKey, string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                _cache = new List<FeederDevice>();
                return _cache;
            }

            try
            {
                _cache = JsonSerializer.Deserialize<List<FeederDevice>>(json) ?? new List<FeederDevice>();
            }
            catch
            {
                _cache = new List<FeederDevice>();
            }

            return _cache;
        }

        public void AddFeeder(FeederDevice feeder)
        {
            var feeders = GetFeeders();
            
            // Remove existing with same ID (update scenario)
            feeders.RemoveAll(f => f.UniqueId == feeder.UniqueId);
            feeders.Add(feeder);

            Save(feeders);
        }

        public void RemoveFeeder(string uniqueId)
        {
            var feeders = GetFeeders();
            feeders.RemoveAll(f => f.UniqueId == uniqueId);
            Save(feeders);
        }

        private void Save(List<FeederDevice> feeders)
        {
            _cache = feeders;
            var json = JsonSerializer.Serialize(feeders);
            Preferences.Set(StorageKey, json);
        }
    }
}
