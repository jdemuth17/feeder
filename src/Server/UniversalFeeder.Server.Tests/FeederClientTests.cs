using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using MQTTnet;
using System.Threading.Tasks;
using UniversalFeeder.Server.Services;
using UniversalFeeder.Shared;
using Xunit;

namespace UniversalFeeder.Server.Tests
{
    public class FeederClientTests
    {
        [Fact]
        public async Task TriggerFeedAsync_ShouldBuildCorrectTopic()
        {
            // Arrange
            var identifier = "Feeder123";
            var expected = "feeders/Feeder123/commands";

            // Act
            var actual = MqttCommands.GetCommandTopic(identifier);

            // Assert
            Assert.Equal(expected, actual);
        }
    }
}
