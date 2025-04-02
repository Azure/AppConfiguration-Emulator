using System;

namespace Azure.AppConfiguration.Emulator.ConfigurationSnapshots
{
    [Flags]
    public enum SnapshotFields
    {
        None = 0,

        Etag = 0x001,
        Name = 0x002,
        Status = 0x004,
        Filters = 0x008,
        CompositionType = 0x010,
        Created = 0x020,
        Expires = 0x040,
        Size = 0x080,
        ItemsCount = 0x100,
        Tags = 0x200,
        RetentionPeriod = 0x400,

        All = Etag | Name | Status | Filters | CompositionType | Created | Expires | Size | ItemsCount | Tags | RetentionPeriod
    }
}
