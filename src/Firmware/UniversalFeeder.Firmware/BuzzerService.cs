using System;
#if NANOFRAMEWORK
using System.Device.Pwm;
#endif

namespace UniversalFeeder.Firmware
{
    public class BuzzerService : IBuzzerService, IDisposable
    {
#if NANOFRAMEWORK
        private readonly PwmChannel _pwm;
#endif

        public BuzzerService()
        {
#if NANOFRAMEWORK
            // ESP32 PWM configuration
            _pwm = PwmChannel.CreateFromPin(HardwareConfig.BuzzerPin, 2000); // 2kHz
#endif
        }

        public void Play(float volume, int durationMs)
        {
#if NANOFRAMEWORK
            _pwm.DutyCycle = volume * 0.5f; // Max 50% duty cycle for buzzer
            _pwm.Start();
#endif
            
            System.Threading.Thread.Sleep(durationMs);
            
#if NANOFRAMEWORK
            _pwm.Stop();
#endif
        }

        public void Dispose()
        {
#if NANOFRAMEWORK
            _pwm?.Dispose();
#endif
        }
    }
}
