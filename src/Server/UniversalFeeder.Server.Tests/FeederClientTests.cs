using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UniversalFeeder.Server.Services;
using Xunit;

namespace UniversalFeeder.Server.Tests
{
    public class FeederClientTests
    {
        [Fact]
        public async Task TriggerFeedAsync_ShouldReturnTrue_OnSuccess()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>()
               )
               .ReturnsAsync(new HttpResponseMessage
               {
                   StatusCode = HttpStatusCode.OK,
                   Content = new StringContent("Success"),
               })
               .Verifiable();

            var httpClient = new HttpClient(handlerMock.Object);
            var feederClient = new FeederClient(httpClient);

            // Act
            var result = await feederClient.TriggerFeedAsync("192.168.1.100", 5000);

            // Assert
            Assert.True(result);
            handlerMock.Protected().Verify(
               "SendAsync",
               Times.Exactly(1),
               ItExpr.Is<HttpRequestMessage>(req =>
                  req.Method == HttpMethod.Get &&
                  req.RequestUri!.ToString() == "http://192.168.1.100/feed?ms=5000"),
               ItExpr.IsAny<CancellationToken>()
            );
        }

        [Fact]
        public async Task TriggerChimeAsync_ShouldReturnTrue_OnSuccess()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>()
               )
               .ReturnsAsync(new HttpResponseMessage
               {
                   StatusCode = HttpStatusCode.OK,
                   Content = new StringContent("Success"),
               })
               .Verifiable();

            var httpClient = new HttpClient(handlerMock.Object);
            var feederClient = new FeederClient(httpClient);

            // Act
            var result = await feederClient.TriggerChimeAsync("192.168.1.100", 1.0f);

            // Assert
            Assert.True(result);
            handlerMock.Protected().Verify(
               "SendAsync",
               Times.Exactly(1),
               ItExpr.Is<HttpRequestMessage>(req =>
                  req.Method == HttpMethod.Get &&
                  req.RequestUri!.ToString() == "http://192.168.1.100/chime?vol=1"),
               ItExpr.IsAny<CancellationToken>()
            );
        }
    }
}
