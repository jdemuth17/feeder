using Microsoft.EntityFrameworkCore;
using UniversalFeeder.Server.Data;
using UniversalFeeder.Server.Models;

namespace UniversalFeeder.Server.Services
{
    public interface ISettingsService
    {
        Task<string> GetSettingAsync(string key, string defaultValue = "");
        Task SaveSettingAsync(string key, string value);
    }

    public class SettingsService : ISettingsService
    {
        private readonly IDbContextFactory<FeederContext> _dbFactory;

        public SettingsService(IDbContextFactory<FeederContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<string> GetSettingAsync(string key, string defaultValue = "")
        {
            using var context = _dbFactory.CreateDbContext();
            var setting = await context.Settings.FindAsync(key);
            return setting?.Value ?? defaultValue;
        }

        public async Task SaveSettingAsync(string key, string value)
        {
            using var context = _dbFactory.CreateDbContext();
            var setting = await context.Settings.FindAsync(key);
            
            if (setting == null)
            {
                context.Settings.Add(new SystemSetting { Key = key, Value = value });
            }
            else
            {
                setting.Value = value;
                context.Settings.Update(setting);
            }

            await context.SaveChangesAsync();
        }
    }
}
