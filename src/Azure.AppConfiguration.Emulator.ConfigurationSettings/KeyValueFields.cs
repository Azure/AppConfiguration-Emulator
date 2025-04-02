// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Azure.AppConfiguration.Emulator.ConfigurationSettings
{
    [Flags]
    public enum KeyValueFields
    {
        None = 0,

        Key = 0x001,
        Label = 0x002,
        ContentType = 0x004,
        Value = 0x008,
        LastModified = 0x010,
        Tags = 0x020,
        Etag = 0x040,
        Locked = 0x080,

        Id = 0x100,
        Deleted = 0x200,
        Ttl = 0x400,
        RevisionTTL = 0x800,
        Revision = 0x1000,
        Name = 0x2000,

        Default = Key | Label | ContentType | Value | LastModified | Tags | Etag | Locked,
        All = Default | Id | Deleted | Ttl | Revision | RevisionTTL | Name
    }
}
