using System;
using System.Threading;

namespace UniversalFeeder.Firmware
{
    public class FeedingSequenceService : IFeedingSequenceService
    {
        private readonly IMotorService _motorService;
        private readonly IBuzzerService _buzzerService;

        public FeedingSequenceService(IMotorService motorService, IBuzzerService buzzerService)
        {
            _motorService = motorService;
            _buzzerService = buzzerService;
        }

        public void Execute(int durationMs)
        {
            // 1. Chime (Buzzer)
            // Volume 1.0, Duration 500ms (Example chime)
            _buzzerService.Play(1.0f, 500);

            // 2. Short Delay (3 seconds as per requirements)
            Thread.Sleep(3000);

            // 3. Motor Spin
            _motorService.Rotate(durationMs);
        }
    }
}
