namespace Azure.AppConfiguration.Emulator.ConfigurationSnapshots
{
    public class SnapshotSearchOptions
    {
        public string Name { get; set; }

        public SnapshotStatusSearch Status { get; set; }

        public string ContinuationToken { get; set; }
    }
}
