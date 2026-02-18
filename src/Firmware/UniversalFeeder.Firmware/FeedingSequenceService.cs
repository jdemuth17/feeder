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
            // 1. Chime (Buzzer) 3 times for 3 seconds each with 1s pause
            for (int i = 0; i < 3; i++)
            {
                _buzzerService.Play(1.0f, 3000);
                
                if (i < 2) // Pause between chimes (not after the last one)
                {
                    Thread.Sleep(1000);
                }
            }

            // 2. Motor Spin
            _motorService.Rotate(durationMs);
        }
    }
}
