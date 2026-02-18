using System;

namespace UniversalFeeder.Firmware
{
    public interface IFeedingSequenceService
    {
        void Execute(int durationMs);
    }
}
