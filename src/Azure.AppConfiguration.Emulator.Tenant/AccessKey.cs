// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Azure.AppConfiguration.Emulator.Tenant
{
    public class AccessKey
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public byte[] Secret { get; set; }

        public bool ReadOnly { get; set; }

        public string EncryptionKeyId { get; set; }

        public DateTimeOffset LastModified { get; set; }
    }
}
