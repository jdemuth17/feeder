using UniversalFeeder.Firmware;

namespace UniversalFeeder.Firmware.Tests
{
    public class RequestProcessorTests
    {
        [Fact]
        public void ParseQueryString_ShouldExtractParameters()
        {
            // Arrange
            string url = "/feed?ms=5000&mode=fast";

            // Act
            var result = RequestProcessor.ParseQueryString(url);

            // Assert
            Assert.Equal("5000", result["ms"]);
            Assert.Equal("fast", result["mode"]);
        }

        [Fact]
        public void ParseQueryString_ShouldHandleNoParameters()
        {
            // Arrange
            string url = "/status";

            // Act
            var result = RequestProcessor.ParseQueryString(url);

            // Assert
            Assert.Empty(result);
        }
    }
}
