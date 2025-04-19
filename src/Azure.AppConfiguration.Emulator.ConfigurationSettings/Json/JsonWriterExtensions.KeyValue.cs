// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Text.Json;
using JsonFields = Azure.AppConfiguration.Emulator.ConfigurationSettings.KeyValueJsonFields;

namespace Azure.AppConfiguration.Emulator.ConfigurationSettings
{
    internal static partial class Utf8JsonWriterExtensions
    {
        public static void WriteKeyValue(this Utf8JsonWriter writer, KeyValue kv)
        {
            if (kv == null)
            {
                throw new ArgumentNullException(nameof(kv));
            }

            writer.WriteStartObject();

            //
            // etag
            if (kv.Etag != null)
            {
                writer.WriteString(JsonFields.Etag, kv.Etag);
            }

            //
            // key
            if (kv.Key != null)
            {
                writer.WriteString(JsonFields.Key, kv.Key);
            }

            //
            // label
            if (kv.Label != null)
            {
                writer.WriteString(JsonFields.Label, kv.Label);
            }

            //
            // content_type
            if (kv.ContentType != null)
            {
                writer.WriteString(JsonFields.ContentType, kv.ContentType);
            }

            //
            // value
            if (kv.Value != null)
            {
                writer.WriteString(JsonFields.Value, kv.Value);
            }

            //
            // created
            writer.WriteNumber(JsonFields.Created, kv.Created.ToUnixTimeSeconds());

            //
            // tags
            if (kv.Tags != null)
            {
                writer.WriteDictionary(JsonFields.Tags, kv.Tags);
            }

            //
            // locked
            if (kv.Locked)
            {
                writer.WriteBoolean(JsonFields.Locked, kv.Locked);
            }

            //
            // deleted
            if (kv.Deleted != null)
            {
                writer.WriteNumber(JsonFields.Deleted, kv.Deleted.Value.ToUnixTimeSeconds());
            }

            //
            // revision_ttl
            if (kv.RevisionTTL > TimeSpan.Zero)
            {
                writer.WriteNumber(JsonFields.RevisionTTL, (long)kv.RevisionTTL.TotalSeconds);
            }

            writer.WriteEndObject();
        }
    }
}
