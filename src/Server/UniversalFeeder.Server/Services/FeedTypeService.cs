using Microsoft.EntityFrameworkCore;
using UniversalFeeder.Server.Data;
using UniversalFeeder.Server.Models;

namespace UniversalFeeder.Server.Services
{
    public interface IFeedTypeService
    {
        Task<List<FeedType>> GetFeedTypesAsync();
        Task<FeedType?> GetFeedTypeByIdAsync(int id);
        Task<FeedType> SaveFeedTypeAsync(FeedType feedType);
        Task DeleteFeedTypeAsync(int id);
    }

    public class FeedTypeService : IFeedTypeService
    {
        private readonly IDbContextFactory<FeederContext> _dbFactory;
        private readonly ILogger<FeedTypeService> _logger;

        public FeedTypeService(IDbContextFactory<FeederContext> dbFactory, ILogger<FeedTypeService> logger)
        {
            _dbFactory = dbFactory;
            _logger = logger;
        }

        public async Task<List<FeedType>> GetFeedTypesAsync()
        {
            try
            {
                using var context = _dbFactory.CreateDbContext();
                return await context.FeedTypes.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving feed types");
                return new List<FeedType>();
            }
        }

        public async Task<FeedType?> GetFeedTypeByIdAsync(int id)
        {
            try
            {
                using var context = _dbFactory.CreateDbContext();
                return await context.FeedTypes.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving feed type with ID {Id}", id);
                return null;
            }
        }

        public async Task<FeedType> SaveFeedTypeAsync(FeedType feedType)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving feed type {Name}", feedType.Name);
                throw;
            }
        }

        public async Task DeleteFeedTypeAsync(int id)
        {
            try
            {
                using var context = _dbFactory.CreateDbContext();
                var feedType = await context.FeedTypes.FindAsync(id);
                if (feedType != null)
                {
                    context.FeedTypes.Remove(feedType);
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting feed type with ID {Id}", id);
                throw;
            }
        }
    }
}
