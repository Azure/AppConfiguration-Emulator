// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Text.Json;

namespace Azure.AppConfiguration.Emulator.ConfigurationSettings
{
    using JsonFields = KeyValueJsonFields;

    public static partial class Utf8JsonReaderExtensions
    {
        public static bool TryReadKeyValue(
            this ref Utf8JsonReader reader,
            out KeyValue keyValue)
        {
            keyValue = null;

            int depth = (reader.TokenType == JsonTokenType.StartObject) ? 1 : 0;

            KeyValue kv = null;

            while (reader.Read())
            {
                //
                // Read top-level properties
                if (depth == 1 &&
                    reader.TokenType == JsonTokenType.PropertyName &&
                    !reader.HasValueSequence)
                {
                    kv ??= new KeyValue();

                    kv.SetProperty(reader.ValueSpan, ref reader);

                    continue;
                }

                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    ++depth;

                    continue;
                }

                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    if (--depth == 0)
                    {
                        break;
                    }
                }
            }

            if (depth == 0)
            {
                keyValue = kv;
            }

            return keyValue != null;
        }

        private static void SetProperty(
            this KeyValue kv,
            ReadOnlySpan<byte> propertyName,
            ref Utf8JsonReader reader)
        {
            //
            // ts
            if (propertyName.IsEqual(JsonFields.Timestamp) &&
                reader.Read())
            {
                kv.Timestamp = DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64());

                return;
            }

            //
            // etag
            if (propertyName.IsEqual(JsonFields.Etag) &&
                reader.Read())
            {
                kv.Etag = reader.GetString();

                return;
            }

            //
            // key
            if (propertyName.IsEqual(JsonFields.Key) &&
                reader.Read())
            {
                kv.Key = reader.GetString();

                return;
            }

            //
            // label
            if (propertyName.IsEqual(JsonFields.Label) &&
                reader.Read())
            {
                kv.Label = reader.GetString();

                return;
            }

            //
            // content_type
            if (propertyName.IsEqual(JsonFields.ContentType) &&
                reader.Read())
            {
                kv.ContentType = reader.GetString();

                return;
            }

            //
            // value
            if (propertyName.IsEqual(JsonFields.Value) &&
                reader.Read())
            {
                kv.Value = reader.GetString();

                return;
            }

            //
            // tags
            if (propertyName.IsEqual(JsonFields.Tags) &&
                reader.Read())
            {
                kv.Tags = reader.ReadDictionary();

                return;
            }

            //
            // locked
            if (propertyName.IsEqual(JsonFields.Locked) &&
                reader.Read())
            {
                kv.Locked = reader.GetBoolean();

                return;
            }

            //
            // rev_ttl
            if (propertyName.IsEqual(JsonFields.RevisionTTL) &&
                reader.Read())
            {
                kv.RevisionTTL = TimeSpan.FromSeconds(reader.GetInt64());

                return;
            }

            //
            // deleted
            if (propertyName.IsEqual(JsonFields.Deleted) &&
                reader.Read())
            {
                kv.Deleted = DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64());

                return;
            }
        }
    }
}
