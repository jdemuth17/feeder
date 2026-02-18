using System;

namespace UniversalFeeder.Firmware
{
    public interface IBuzzerService
    {
        void Play(float volume, int durationMs);
    }
}
