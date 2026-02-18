using Moq;
using UniversalFeeder.Firmware;

namespace UniversalFeeder.Firmware.Tests
{
    public class FeedingSequenceServiceTests
    {
        [Fact]
        public void Execute_ShouldPlayChimeThenRotateMotor()
        {
            // Arrange
            var mockMotor = new Mock<IMotorService>();
            var mockBuzzer = new Mock<IBuzzerService>();
            var sequence = new FeedingSequenceService(mockMotor.Object, mockBuzzer.Object);
            int duration = 5000;

            var callOrder = new List<string>();
            mockBuzzer.Setup(b => b.Play(It.IsAny<float>(), It.IsAny<int>()))
                .Callback(() => callOrder.Add("Buzzer"));
            mockMotor.Setup(m => m.Rotate(It.IsAny<int>()))
                .Callback(() => callOrder.Add("Motor"));

            // Act
            sequence.Execute(duration);

            // Assert
            Assert.Equal(2, callOrder.Count);
            Assert.Equal("Buzzer", callOrder[0]);
            Assert.Equal("Motor", callOrder[1]);
            mockBuzzer.Verify(b => b.Play(1.0f, It.IsAny<int>()), Times.Once);
            mockMotor.Verify(m => m.Rotate(duration), Times.Once);
        }
    }
}
