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
                .ThenInclude(f => f.FeedType)
                .Where(s => s.IsEnabled && s.TimeOfDay >= windowStart && s.TimeOfDay < windowEnd)
                .ToListAsync();

            foreach (var schedule in dueSchedules)
            {
                if (schedule.Feeder == null || string.IsNullOrEmpty(schedule.Feeder.IpAddress))
                {
                    _logger.LogWarning("Schedule {Id} has no valid feeder or IP.", schedule.Id);
                    continue;
                }

                double gramsPerSecond = schedule.Feeder.FeedType?.GramsPerSecond ?? 10.0;
                int durationMs = (int)((schedule.AmountInGrams / gramsPerSecond) * 1000);

                _logger.LogInformation("Triggering scheduled feed for {Nickname} ({Ip}): {Amount}g -> {Duration}ms", 
                    schedule.Feeder.Nickname, schedule.Feeder.IpAddress, schedule.AmountInGrams, durationMs);

                bool success = await _feederClient.TriggerFeedAsync(schedule.Feeder.IpAddress, durationMs);

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
