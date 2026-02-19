using UniversalFeeder.Server.Models;
using Xunit;

namespace UniversalFeeder.Server.Tests
{
    public class FeedingScheduleTests
    {
        [Fact]
        public void FeedingSchedule_ShouldHaveRequiredProperties()
        {
            // Arrange & Act
            var schedule = new FeedingSchedule
            {
                Id = 1,
                FeederId = 10,
                TimeOfDay = new TimeSpan(8, 30, 0),
                AmountInGrams = 250.5,
                IsEnabled = true
            };

            // Assert
            Assert.Equal(1, schedule.Id);
            Assert.Equal(10, schedule.FeederId);
            Assert.Equal(new TimeSpan(8, 30, 0), schedule.TimeOfDay);
            Assert.Equal(250.5, schedule.AmountInGrams);
            Assert.True(schedule.IsEnabled);
        }
    }
}
