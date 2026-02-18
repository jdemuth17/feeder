using System;

namespace UniversalFeeder.Firmware
{
    /// <summary>
    /// Service for controlling the Nema 17 motor.
    /// </summary>
    public interface IMotorService
    {
        /// <summary>
        /// Rotates the motor for the specified duration.
        /// </summary>
        /// <param name="durationMs">Duration in milliseconds.</param>
        void Rotate(int durationMs);

        /// <summary>
        /// Gets whether the motor is currently rotating.
        /// </summary>
        bool IsRotating { get; }
    }
}
