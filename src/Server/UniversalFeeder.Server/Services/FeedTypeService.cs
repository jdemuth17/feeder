using Microsoft.EntityFrameworkCore;
using UniversalFeeder.Server.Data;
using UniversalFeeder.Server.Models;

namespace UniversalFeeder.Server.Services
{
    public interface IFeedTypeService
    {
        Task<List<FeedType>> GetFeedTypesAsync();
        Task<FeedType> GetFeedTypeByIdAsync(int id);
        Task<FeedType> SaveFeedTypeAsync(FeedType feedType);
        Task DeleteFeedTypeAsync(int id);
    }

    public class FeedTypeService : IFeedTypeService
    {
        private readonly IDbContextFactory<FeederContext> _dbFactory;

        public FeedTypeService(IDbContextFactory<FeederContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<List<FeedType>> GetFeedTypesAsync()
        {
            using var context = _dbFactory.CreateDbContext();
            return await context.FeedTypes.ToListAsync();
        }

        public async Task<FeedType> GetFeedTypeByIdAsync(int id)
        {
            using var context = _dbFactory.CreateDbContext();
            return await context.FeedTypes.FindAsync(id);
        }

        public async Task<FeedType> SaveFeedTypeAsync(FeedType feedType)
        {
            using var context = _dbFactory.CreateDbContext();
            if (feedType.Id == 0)
            {
                context.FeedTypes.Add(feedType);
            }
            else
            {
                context.FeedTypes.Update(feedType);
            }
            await context.SaveChangesAsync();
            return feedType;
        }

        public async Task DeleteFeedTypeAsync(int id)
        {
            using var context = _dbFactory.CreateDbContext();
            var feedType = await context.FeedTypes.FindAsync(id);
            if (feedType != null)
            {
                context.FeedTypes.Remove(feedType);
                await context.SaveChangesAsync();
            }
        }
    }
}
