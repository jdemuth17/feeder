using Microsoft.EntityFrameworkCore;
using UniversalFeeder.Server.Data;
using UniversalFeeder.Server.Models;

namespace UniversalFeeder.Server.Services
{
    public interface IFeederService
    {
        Task<List<Feeder>> GetFeedersAsync();
        Task<Feeder?> GetFeederByIdAsync(int id);
        Task<Feeder> SaveFeederAsync(Feeder feeder);
        Task AddFeedingLogAsync(FeedingLog log);
    }

    public class FeederService : IFeederService
    {
        private readonly IDbContextFactory<FeederContext> _dbFactory;
        private readonly ILogger<FeederService> _logger;

        public FeederService(IDbContextFactory<FeederContext> dbFactory, ILogger<FeederService> logger)
        {
            _dbFactory = dbFactory;
            _logger = logger;
        }

        public async Task<List<Feeder>> GetFeedersAsync()
        {
            try
            {
                using var context = _dbFactory.CreateDbContext();
                return await context.Feeders.Include(f => f.FeedType).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving feeders");
                return new List<Feeder>();
            }
        }

        public async Task<Feeder?> GetFeederByIdAsync(int id)
        {
            try
            {
                using var context = _dbFactory.CreateDbContext();
                return await context.Feeders.Include(f => f.FeedType).FirstOrDefaultAsync(f => f.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving feeder with ID {Id}", id);
                return null;
            }
        }

        public async Task<Feeder> SaveFeederAsync(Feeder feeder)
        {
            try
            {
                using var context = _dbFactory.CreateDbContext();
                var existing = await context.Feeders.FirstOrDefaultAsync(f => f.UniqueId == feeder.UniqueId || (feeder.Id != 0 && f.Id == feeder.Id));

                if (existing != null)
                {
                    existing.Nickname = feeder.Nickname;
                    existing.IpAddress = feeder.IpAddress;
                    existing.FeedTypeId = feeder.FeedTypeId;
                    context.Feeders.Update(existing);
                }
                else
                {
                    context.Feeders.Add(feeder);
                }
                await context.SaveChangesAsync();
                return feeder;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving feeder {Nickname}", feeder.Nickname);
                throw;
            }
        }

        public async Task AddFeedingLogAsync(FeedingLog log)
        {
            try
            {
                using var context = _dbFactory.CreateDbContext();
                context.Logs.Add(log);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding feeding log for feeder {Id}", log.FeederId);
            }
        }
    }
}
