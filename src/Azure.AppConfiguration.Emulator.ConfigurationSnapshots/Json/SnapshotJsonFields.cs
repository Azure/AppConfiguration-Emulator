// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Text.Json;

namespace Azure.AppConfiguration.Emulator.ConfigurationSnapshots
{
    internal static class SnapshotJsonFields
    {
        public static readonly JsonEncodedText Id = JsonEncodedText.Encode("id");
        public static readonly JsonEncodedText Name = JsonEncodedText.Encode("name");
        public static readonly JsonEncodedText Etag = JsonEncodedText.Encode("etag");
        public static readonly JsonEncodedText Status = JsonEncodedText.Encode("status");
        public static readonly JsonEncodedText StatusCode = JsonEncodedText.Encode("status_code");
        public static readonly JsonEncodedText CompositionType = JsonEncodedText.Encode("composition_type");
        public static readonly JsonEncodedText RetentionPeriodSeconds = JsonEncodedText.Encode("retention_period_seconds");
        public static readonly JsonEncodedText Created = JsonEncodedText.Encode("created");
        public static readonly JsonEncodedText LastModified = JsonEncodedText.Encode("last_modified");
        public static readonly JsonEncodedText Expires = JsonEncodedText.Encode("expires");
        public static readonly JsonEncodedText ItemsCount = JsonEncodedText.Encode("items_count");
        public static readonly JsonEncodedText SizeBytes = JsonEncodedText.Encode("size_bytes");
        public static readonly JsonEncodedText Tags = JsonEncodedText.Encode("tags");
        public static readonly JsonEncodedText Filters = JsonEncodedText.Encode("filters");
        public static readonly JsonEncodedText Media = JsonEncodedText.Encode("media");
        // Nested media fields
        public static readonly JsonEncodedText Category = JsonEncodedText.Encode("category");
        public static readonly JsonEncodedText MediaName = JsonEncodedText.Encode("name");
        public static readonly JsonEncodedText MediaSize = JsonEncodedText.Encode("size");
        public static readonly JsonEncodedText ContentType = JsonEncodedText.Encode("content_type");
        public static readonly JsonEncodedText Sha256 = JsonEncodedText.Encode("sha256");
        // Filter item fields
        public static readonly JsonEncodedText Key = JsonEncodedText.Encode("key");
        public static readonly JsonEncodedText Label = JsonEncodedText.Encode("label");
    }
}
