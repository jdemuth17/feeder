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

        public FeederService(IDbContextFactory<FeederContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<List<Feeder>> GetFeedersAsync()
        {
            using var context = _dbFactory.CreateDbContext();
            return await context.Feeders.Include(f => f.FeedType).ToListAsync();
        }

        public async Task<Feeder?> GetFeederByIdAsync(int id)
        {
            using var context = _dbFactory.CreateDbContext();
            return await context.Feeders.Include(f => f.FeedType).FirstOrDefaultAsync(f => f.Id == id);
        }

        public async Task<Feeder> SaveFeederAsync(Feeder feeder)
        {
            using var context = _dbFactory.CreateDbContext();
            if (feeder.Id == 0)
            {
                context.Feeders.Add(feeder);
            }
            else
            {
                context.Feeders.Update(feeder);
            }
            await context.SaveChangesAsync();
            return feeder;
        }

        public async Task AddFeedingLogAsync(FeedingLog log)
        {
            using var context = _dbFactory.CreateDbContext();
            context.Logs.Add(log);
            await context.SaveChangesAsync();
        }
    }
}
