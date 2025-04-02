using System;

namespace Azure.AppConfiguration.Emulator.ConfigurationSnapshots
{
    public class SnapshotNotFoundException : Exception
    {
        public SnapshotNotFoundException(
            Exception inner = null)
            : base(string.Empty, inner)
        {
        }
    }
}
