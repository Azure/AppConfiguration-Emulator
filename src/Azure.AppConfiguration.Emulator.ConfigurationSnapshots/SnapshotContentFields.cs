namespace Azure.AppConfiguration.Emulator.ConfigurationSnapshots
{
    /// <summary>
    /// Don't modify existing values
    /// These correspond to persisted data entries or protocol
    /// </summary>
    static class SnapshotContentFields
    {
        public const string Key = "key";
        public const string Label = "label";
        public const string Value = "value";
        public const string Content = "content";
        public const string Items = "items";
        public const string ContentType = "content_type";
        public const string Created = "created";
        public const string Tags = "tags";
        public const string Locked = "locked";
        public const string ETag = "etag";
    }
}
