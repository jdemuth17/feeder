using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Quartz;
using UniversalFeeder.Server.Data;
using UniversalFeeder.Server.Jobs;
using UniversalFeeder.Server.Models;
using UniversalFeeder.Server.Services;
using Xunit;

namespace UniversalFeeder.Server.Tests
{
    public class FeedingJobTests
    {
        [Fact]
        public async Task Execute_ShouldTriggerFeeder_WhenScheduleIsDue()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<FeederContext>()
                .UseInMemoryDatabase(databaseName: "FeedingJobTest")
                .Options;

            var mockDbFactory = new Mock<IDbContextFactory<FeederContext>>();
            mockDbFactory.Setup(f => f.CreateDbContext()).Returns(() => new FeederContext(options));

            var mockFeederClient = new Mock<IFeederClient>();
            var mockLogger = new Mock<ILogger<FeedingJob>>();
            var mockContext = new Mock<IJobExecutionContext>();

            using (var context = new FeederContext(options))
            {
                var feedType = new FeedType { Id = 1, Name = "Test", GramsPerSecond = 10 };
                var feeder = new Feeder { Id = 1, Nickname = "Feeder1", IpAddress = "1.1.1.1", FeedTypeId = 1 };
                var now = DateTime.Now.TimeOfDay;
                var schedule = new FeedingSchedule 
                { 
                    Id = 1, 
                    FeederId = 1, 
                    TimeOfDay = new TimeSpan(now.Hours, now.Minutes, 0), 
                    AmountInGrams = 100, 
                    IsEnabled = true 
                };

                context.FeedTypes.Add(feedType);
                context.Feeders.Add(feeder);
                context.Schedules.Add(schedule);
                await context.SaveChangesAsync();
            }

            var job = new FeedingJob(mockDbFactory.Object, mockFeederClient.Object, mockLogger.Object);

            // Act
            await job.Execute(mockContext.Object);

            // Assert
            // 100g / 10g/s = 10s = 10000ms
            mockFeederClient.Verify(c => c.TriggerFeedAsync("1.1.1.1", 10000), Times.Once);
            
            using (var context = new FeederContext(options))
            {
                var log = await context.Logs.FirstOrDefaultAsync();
                Assert.NotNull(log);
                Assert.Equal(1, log.FeederId);
                Assert.False(log.IsManualOverride);
            }
        }
    }
}
