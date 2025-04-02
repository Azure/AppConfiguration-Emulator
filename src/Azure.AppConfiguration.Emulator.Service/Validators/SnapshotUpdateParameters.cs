using Azure.AppConfiguration.Emulator.ConfigurationSnapshots;
using Microsoft.AppConfig.Service;

namespace Azure.AppConfiguration.Emulator.Service.Validators
{
    public class SnapshotUpdateParameters
    {
        [AllowedValues(SnapshotStatus.Ready, SnapshotStatus.Archived)]
        public SnapshotStatus Status { get; set; }
    }
}
