using Microsoft.EntityFrameworkCore;
using Quartz;
using UniversalFeeder.Server.Data;
using UniversalFeeder.Server.Models;
using UniversalFeeder.Server.Services;

namespace UniversalFeeder.Server.Jobs
{
    public class FeedingJob : IJob
    {
        private readonly IDbContextFactory<FeederContext> _dbFactory;
        private readonly IFeederClient _feederClient;
        private readonly ILogger<FeedingJob> _logger;

        public FeedingJob(IDbContextFactory<FeederContext> dbFactory, IFeederClient feederClient, ILogger<FeedingJob> logger)
        {
            _dbFactory = dbFactory;
            _feederClient = feederClient;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var now = DateTime.Now.TimeOfDay;
            var windowStart = new TimeSpan(now.Hours, now.Minutes, 0);
            var windowEnd = windowStart.Add(TimeSpan.FromMinutes(1));

            _logger.LogInformation("FeedingJob checking for schedules between {Start} and {End}", windowStart, windowEnd);

            using var dbContext = _dbFactory.CreateDbContext();

            var dueSchedules = await dbContext.Schedules
                .Include(s => s.Feeder)
                .ThenInclude(feeder => feeder!.FeedType)
                .Where(s => s.IsEnabled && s.TimeOfDay >= windowStart && s.TimeOfDay < windowEnd)
                .ToListAsync();

            foreach (var schedule in dueSchedules)
            {
                if (schedule.Feeder == null || string.IsNullOrWhiteSpace(schedule.Feeder.UniqueId))
                {
                    _logger.LogWarning("Schedule {Id} has no valid feeder identifier.", schedule.Id);
                    continue;
                }

                double gramsPerSecond = schedule.Feeder.FeedType?.GramsPerSecond ?? 10.0;
                if (gramsPerSecond <= 0)
                {
                    _logger.LogWarning("Schedule {Id} has invalid feed rate {Rate}g/s.", schedule.Id, gramsPerSecond);
                    continue;
                }

                int durationMs = (int)((schedule.AmountInGrams / gramsPerSecond) * 1000);

                _logger.LogInformation("Triggering scheduled feed for {Nickname} ({UniqueId}): {Amount}g -> {Duration}ms", 
                    schedule.Feeder.Nickname, schedule.Feeder.UniqueId, schedule.AmountInGrams, durationMs);

                bool success = await _feederClient.TriggerFeedAsync(schedule.Feeder.UniqueId, durationMs);

                dbContext.Logs.Add(new FeedingLog
                {
                    FeederId = schedule.FeederId,
                    Timestamp = DateTime.UtcNow,
                    Success = success,
                    IsManualOverride = false,
                    Status = success ? $"Scheduled feed success ({schedule.AmountInGrams}g)" : "Scheduled feed failed"
                });
            }

            await dbContext.SaveChangesAsync();
        }
    }
}
