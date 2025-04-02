using System;
using System.Collections.Generic;

namespace Azure.AppConfiguration.Emulator.ConfigurationSnapshots
{
    public class Snapshot
    {
        public string Id { get; set; }

        public string Etag { get; set; }

        public string Name { get; set; }

        public IEnumerable<KeyValueFilter> Filters { get; set; }

        public CompositionType CompositionType { get; set; }

        public SnapshotStatus Status { get; set; }

        public MediaInfo Media { get; set; }

        public long ItemCount { get; set; }

        public long Size { get; set; }

        public TimeSpan RetentionPeriod { get; set; }

        public int StatusCode { get; set; }

        public DateTimeOffset Created { get; set; }

        public DateTimeOffset? Expires { get; set; }

        public DateTimeOffset LastModified { get; set; }

        public IDictionary<string, string> Tags { get; set; }
    }
}
