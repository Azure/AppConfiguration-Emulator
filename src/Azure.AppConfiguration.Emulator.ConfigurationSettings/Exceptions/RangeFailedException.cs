using System;

namespace Azure.AppConfiguration.Emulator.ConfigurationSettings
{
    public class RangeFailedException : Exception
    {
        public RangeFailedException() : base()
        {
        }

        public RangeFailedException(string message, Exception inner = null) : base(message, inner)
        {
        }
    }
}
