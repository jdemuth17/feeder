namespace UniversalFeeder.Firmware
{
    /// <summary>
    /// Hardware configuration for the ESP32 feeder.
    /// </summary>
    public static class HardwareConfig
    {
        // A4988 Driver Pins
        public const int StepPin = 14;
        public const int DirPin = 12;
        public const int EnablePin = 13;

        // Buzzer Pin (PWM)
        public const int BuzzerPin = 27;

        // Feed Level Sensor (Example)
        public const int HopperSensorPin = 34;
    }
}
