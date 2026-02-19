using Microsoft.EntityFrameworkCore;
using UniversalFeeder.Server.Data;
using UniversalFeeder.Server.Models;

namespace UniversalFeeder.Server.Services
{
    public interface IScheduleService
    {
        Task<List<FeedingSchedule>> GetSchedulesByFeederIdAsync(int feederId);
        Task<FeedingSchedule> SaveScheduleAsync(FeedingSchedule schedule);
        Task DeleteScheduleAsync(int id);
    }

    public class ScheduleService : IScheduleService
    {
        private readonly IDbContextFactory<FeederContext> _dbFactory;
        private readonly ILogger<ScheduleService> _logger;

        public ScheduleService(IDbContextFactory<FeederContext> dbFactory, ILogger<ScheduleService> logger)
        {
            _dbFactory = dbFactory;
            _logger = logger;
        }

        public async Task<List<FeedingSchedule>> GetSchedulesByFeederIdAsync(int feederId)
        {
            try
            {
                using var context = _dbFactory.CreateDbContext();
                return await context.Schedules
                    .Where(s => s.FeederId == feederId)
                    .OrderBy(s => s.TimeOfDay)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving schedules for feeder {Id}", feederId);
                return new List<FeedingSchedule>();
            }
        }

        public async Task<FeedingSchedule> SaveScheduleAsync(FeedingSchedule schedule)
        {
            try
            {
                using var context = _dbFactory.CreateDbContext();
                if (schedule.Id == 0)
                {
                    context.Schedules.Add(schedule);
                }
                else
                {
                    context.Schedules.Update(schedule);
                }
                await context.SaveChangesAsync();
                return schedule;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving schedule for feeder {Id}", schedule.FeederId);
                throw;
            }
        }

        public async Task DeleteScheduleAsync(int id)
        {
            try
            {
                using var context = _dbFactory.CreateDbContext();
                var schedule = await context.Schedules.FindAsync(id);
                if (schedule != null)
                {
                    context.Schedules.Remove(schedule);
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting schedule with ID {Id}", id);
                throw;
            }
        }
    }
}
