using System.Text.Json;
using UniversalFeeder.Mobile.Models;

namespace UniversalFeeder.Mobile.Services
{
    /// <summary>
    /// Persists named feed type profiles (e.g. "Kibble Dog Food = 5s/cup") to device storage.
    /// Thread-safe for reads; writes happen on the UI thread only.
    /// </summary>
    public class FeedTypeService
    {
        private const string PreferencesKey = "feed_types_v1";
        private List<FeedType> _cache = new();
        private bool _loaded;

        public IReadOnlyList<FeedType> GetAll()
        {
            EnsureLoaded();
            return _cache.AsReadOnly();
        }

        public FeedType? GetById(string id)
        {
            EnsureLoaded();
            return _cache.FirstOrDefault(f => f.Id == id);
        }

        public void Save(FeedType feedType)
        {
            EnsureLoaded();
            var idx = _cache.FindIndex(f => f.Id == feedType.Id);
            if (idx >= 0)
                _cache[idx] = feedType;
            else
                _cache.Add(feedType);
            Persist();
        }

        public void Delete(string id)
        {
            EnsureLoaded();
            _cache.RemoveAll(f => f.Id == id);
            Persist();
        }

        private void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            var json = Preferences.Get(PreferencesKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json)) return;
            try { _cache = JsonSerializer.Deserialize<List<FeedType>>(json) ?? new(); }
            catch { _cache = new(); }
        }

        private void Persist()
        {
            Preferences.Set(PreferencesKey, JsonSerializer.Serialize(_cache));
        }
    }
}
