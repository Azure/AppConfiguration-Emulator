using System;

namespace Azure.AppConfiguration.Emulator.ConfigurationSnapshots
{
    public class ConflictException : Exception
    {
        public ConflictException(Exception inner = null)
            : base(string.Empty, inner)
        {
        }
    }
}
