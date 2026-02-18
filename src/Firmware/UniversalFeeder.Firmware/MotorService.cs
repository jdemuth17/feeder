using System;
using System.Threading;
#if NANOFRAMEWORK
using System.Device.Gpio;
#endif

namespace UniversalFeeder.Firmware
{
    public class MotorService : IMotorService, IDisposable
    {
#if NANOFRAMEWORK
        private readonly GpioController _gpio;
        private readonly GpioPin _stepPin;
        private readonly GpioPin _dirPin;
        private readonly GpioPin _enablePin;
#endif
        private bool _isRotating;

        public bool IsRotating => _isRotating;

        public MotorService()
        {
#if NANOFRAMEWORK
            _gpio = new GpioController();
            
            _stepPin = _gpio.OpenPin(HardwareConfig.StepPin, PinMode.Output);
            _dirPin = _gpio.OpenPin(HardwareConfig.DirPin, PinMode.Output);
            _enablePin = _gpio.OpenPin(HardwareConfig.EnablePin, PinMode.Output);

            // Disable motor by default
            _enablePin.Write(PinValue.High); 
#endif
        }

        public void Rotate(int durationMs)
        {
            _isRotating = true;
            
#if NANOFRAMEWORK
            // Enable motor
            _enablePin.Write(PinValue.Low);
            _dirPin.Write(PinValue.High); // Clockwise
#endif

            DateTime end = DateTime.UtcNow.AddMilliseconds(durationMs);
            
            while (DateTime.UtcNow < end)
            {
#if NANOFRAMEWORK
                _stepPin.Write(PinValue.High);
#endif
                Thread.Sleep(1); // 1ms pulse (basic)
#if NANOFRAMEWORK
                _stepPin.Write(PinValue.Low);
#endif
                Thread.Sleep(1);
            }

#if NANOFRAMEWORK
            // Disable motor
            _enablePin.Write(PinValue.High);
#endif
            _isRotating = false;
        }

        public void Dispose()
        {
#if NANOFRAMEWORK
            _stepPin?.Dispose();
            _dirPin?.Dispose();
            _enablePin?.Dispose();
            _gpio?.Dispose();
#endif
        }
    }
}
