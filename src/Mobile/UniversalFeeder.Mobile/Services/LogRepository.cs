using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using SQLite;
using UniversalFeeder.Mobile.Models;

namespace UniversalFeeder.Mobile.Services
{
    public class LogRepository
    {
        readonly SQLiteAsyncConnection _db;

        public LogRepository()
        {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "universalfeeder.db3");
            _db = new SQLiteAsyncConnection(dbPath);
            _db.CreateTableAsync<FeedingLogDb>().Wait();
        }

        public Task<int> InsertLogAsync(FeedingLogDb log)
        {
            return _db.InsertAsync(log);
        }

        public async Task<List<FeedingLogDb>> GetLogsForFeederAsync(string feederId, int limit = 100)
        {
            if (string.IsNullOrWhiteSpace(feederId)) return new List<FeedingLogDb>();
            var q = await _db.Table<FeedingLogDb>()
                .Where(x => x.FeederId == feederId)
                .OrderByDescending(x => x.TimestampUtc)
                .Take(limit)
                .ToListAsync();
            return q.OrderBy(x => x.TimestampUtc).ToList();
        }

        public Task<int> DeleteOldLogsAsync(DateTime olderThan)
        {
            return _db.ExecuteAsync("DELETE FROM FeedingLogDb WHERE TimestampUtc < ?", olderThan);
        }
    }
}
