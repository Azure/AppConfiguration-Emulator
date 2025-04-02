// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Azure.AppConfiguration.Emulator.ConfigurationSnapshots
{
    public class MediaInfo
    {
        public string Category { get; set; }

        public string Name { get; set; }

        public long Size { get; set; }

        public string Etag { get; set; }

        public string ContentType { get; set; }

        public byte[] Sha256Hash { get; set; }
    }
}
