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

        public ScheduleService(IDbContextFactory<FeederContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<List<FeedingSchedule>> GetSchedulesByFeederIdAsync(int feederId)
        {
            using var context = _dbFactory.CreateDbContext();
            return await context.Schedules
                .Where(s => s.FeederId == feederId)
                .OrderBy(s => s.TimeOfDay)
                .ToListAsync();
        }

        public async Task<FeedingSchedule> SaveScheduleAsync(FeedingSchedule schedule)
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

        public async Task DeleteScheduleAsync(int id)
        {
            using var context = _dbFactory.CreateDbContext();
            var schedule = await context.Schedules.FindAsync(id);
            if (schedule != null)
            {
                context.Schedules.Remove(schedule);
                await context.SaveChangesAsync();
            }
        }
    }
}
