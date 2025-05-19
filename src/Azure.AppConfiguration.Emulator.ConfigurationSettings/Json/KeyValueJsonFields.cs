// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Text.Json;

namespace Azure.AppConfiguration.Emulator.ConfigurationSettings
{
    /// <summary>
    /// Persisted fields
    /// Altering existing values may cause breaking changes with persisted data
    /// </summary>
    public static class KeyValueJsonFields
    {
        public static readonly JsonEncodedText Etag = JsonEncodedText.Encode("etag");
        public static readonly JsonEncodedText Key = JsonEncodedText.Encode("key");
        public static readonly JsonEncodedText Label = JsonEncodedText.Encode("label");
        public static readonly JsonEncodedText ContentType = JsonEncodedText.Encode("content_type");
        public static readonly JsonEncodedText Value = JsonEncodedText.Encode("value");
        public static readonly JsonEncodedText Timestamp = JsonEncodedText.Encode("ts");
        public static readonly JsonEncodedText Tags = JsonEncodedText.Encode("tags");
        public static readonly JsonEncodedText Locked = JsonEncodedText.Encode("locked");
        public static readonly JsonEncodedText Deleted = JsonEncodedText.Encode("deleted");
        public static readonly JsonEncodedText RevisionTTL = JsonEncodedText.Encode("rev_ttl");
    }
}
