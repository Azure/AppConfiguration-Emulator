// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Azure.AppConfiguration.Emulator.ConfigurationSettings
{
    public class KeyValueStorageOptions
    {
        public string FilePath { get; set; } = "c:/aace/kv.ndjson";

        public int AppendBufferSize { get; set; } = 8 * 1024; // bytes

        public int ReadBufferSizeHint { get; set; } = 32 * 1024; // bytes

        public int MaxReadBufferSize { get; set; } = 1_000_000; // bytes
    }
}
