// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Azure.AppConfiguration.Emulator.ConfigurationSnapshots
{
    public class SnapshotsStorageOptions
    {
        public string MetadataFilePath { get; set; } = ".aace/snapshot.ndjson";

        public string ContentDirectory { get; set; } = ".aace/snapshots";

        public int AppendBufferSize { get; set; } = 8 * 1024; // bytes

        public int WriteBufferSize { get; set; } = 1_000_000; // bytes

        public int ReadBufferSizeHint { get; set; } = 32 * 1024; // bytes

        public int MaxReadBufferSize { get; set; } = 1_000_000; // bytes

        public TimeSpan ReadTimeout { get; set; } = TimeSpan.FromSeconds(120);

        public TimeSpan WriteTimeout { get; set; } = TimeSpan.FromSeconds(120);
    }
}
