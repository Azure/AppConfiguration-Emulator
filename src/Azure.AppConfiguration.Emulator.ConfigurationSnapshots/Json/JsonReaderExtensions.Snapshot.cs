// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using Azure.AppConfiguration.Emulator.ConfigurationSettings;

namespace Azure.AppConfiguration.Emulator.ConfigurationSnapshots
{
    internal static class Utf8JsonReaderExtensionsSnapshot
    {
        public static bool TryReadSnapshot(
            this ref Utf8JsonReader reader,
            out Snapshot snapshot)
        {
            snapshot = null;

            int depth = (reader.TokenType == JsonTokenType.StartObject) ? 1 : 0;

            Snapshot ss = null;

            while (reader.Read())
            {
                //
                // Read top-level properties
                if (depth == 1 &&
                    reader.TokenType == JsonTokenType.PropertyName &&
                    !reader.HasValueSequence)
                {
                    ss ??= new Snapshot();

                    ss.SetProperty(reader.ValueSpan, ref reader);

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
                snapshot = ss;
            }

            return snapshot != null;
        }

        private static void SetProperty(
            this Snapshot snapshot,
            ReadOnlySpan<byte> propertyName,
            ref Utf8JsonReader reader)
        {
            //
            // id
            if (propertyName.IsEqual(SnapshotJsonFields.Id) &&
                reader.Read())
            {
                snapshot.Id = reader.GetString();

                return;
            }

            //
            // name
            if (propertyName.IsEqual(SnapshotJsonFields.Name) &&
                reader.Read())
            {
                snapshot.Name = reader.GetString();

                return;
            }

            //
            // etag
            if (propertyName.IsEqual(SnapshotJsonFields.Etag) &&
                reader.Read())
            {
                snapshot.Etag = reader.GetString();

                return;
            }

            //
            // status
            if (propertyName.IsEqual(SnapshotJsonFields.Status) &&
                reader.Read())
            {
                snapshot.Status = ParseStatus(reader.GetString());

                return;
            }

            //
            // status_code
            if (propertyName.IsEqual(SnapshotJsonFields.StatusCode) &&
                reader.Read())
            {
                snapshot.StatusCode = reader.GetInt32();

                return;
            }

            //
            // composition_type
            if (propertyName.IsEqual(SnapshotJsonFields.CompositionType) &&
                reader.Read() &&
                reader.TokenType == JsonTokenType.String)
            {
                snapshot.CompositionType = ParseCompositionType(reader.GetString());

                return;
            }

            //
            // retention_period_seconds
            if (propertyName.IsEqual(SnapshotJsonFields.RetentionPeriodSeconds) &&
                reader.Read())
            {
                snapshot.RetentionPeriod = TimeSpan.FromSeconds(reader.GetInt64());

                return;
            }

            //
            // created
            if (propertyName.IsEqual(SnapshotJsonFields.Created) &&
                reader.Read())
            {
                snapshot.Created = DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64());

                return;
            }

            //
            // last_modified
            if (propertyName.IsEqual(SnapshotJsonFields.LastModified) &&
                reader.Read())
            {
                snapshot.LastModified = DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64());

                return;
            }

            //
            // expires
            if (propertyName.IsEqual(SnapshotJsonFields.Expires) &&
                reader.Read())
            {
                snapshot.Expires = DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64());
                return;
            }

            //
            // items_count
            if (propertyName.IsEqual(SnapshotJsonFields.ItemsCount) &&
                reader.Read())
            {
                snapshot.ItemCount = reader.GetInt64();

                return;
            }

            //
            // size_bytes
            if (propertyName.IsEqual(SnapshotJsonFields.SizeBytes) &&
                reader.Read())
            {
                snapshot.Size = reader.GetInt64();

                return;
            }

            //
            // tags
            if (propertyName.IsEqual(SnapshotJsonFields.Tags) &&
                reader.Read())
            {
                snapshot.Tags = reader.ReadDictionary();

                return;
            }

            //
            // filters
            if (propertyName.IsEqual(SnapshotJsonFields.Filters) &&
                reader.Read() &&
                reader.TokenType == JsonTokenType.StartArray)
            {
                var filters = new List<KeyValueFilter>();

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.StartObject)
                    {
                        string key = null;

                        string label = null;

                        while (reader.Read())
                        {
                            if (reader.TokenType == JsonTokenType.PropertyName && !reader.HasValueSequence)
                            {
                                ReadOnlySpan<byte> pn = reader.ValueSpan;

                                if (pn.IsEqual(SnapshotJsonFields.Key) &&
                                    reader.Read())
                                {
                                    key = reader.GetString();

                                    continue;
                                }

                                if (pn.IsEqual(SnapshotJsonFields.Label) &&
                                    reader.Read())
                                {
                                    label = reader.GetString();

                                    continue;
                                }
                            }

                            if (reader.TokenType == JsonTokenType.EndObject)
                            {
                                break;
                            }
                        }

                        filters.Add(new KeyValueFilter { Key = key, Label = label });
                    }

                    if (reader.TokenType == JsonTokenType.EndArray)
                    {
                        break;
                    }
                }

                snapshot.Filters = filters;

                return;
            }

            //
            // media
            if (propertyName.IsEqual(SnapshotJsonFields.Media) &&
                reader.Read() &&
                reader.TokenType == JsonTokenType.StartObject)
            {
                MediaInfo media = new MediaInfo();

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.PropertyName && !reader.HasValueSequence)
                    {
                        ReadOnlySpan<byte> pn = reader.ValueSpan;

                        //
                        // category
                        if (pn.IsEqual(SnapshotJsonFields.Category) &&
                            reader.Read())
                        {
                            media.Category = reader.GetString();

                            continue;
                        }

                        //
                        // media name
                        if (pn.IsEqual(SnapshotJsonFields.MediaName) &&
                            reader.Read())
                        {
                            media.Name = reader.GetString();

                            continue;
                        }

                        //
                        // media size
                        if (pn.IsEqual(SnapshotJsonFields.MediaSize) &&
                            reader.Read())
                        {
                            media.Size = reader.GetInt64();

                            continue;
                        }

                        //
                        // etag
                        if (pn.IsEqual(SnapshotJsonFields.Etag) &&
                            reader.Read())
                        {
                            media.Etag = reader.GetString();
                            continue;
                        }

                        //
                        // content_type
                        if (pn.IsEqual(SnapshotJsonFields.ContentType) &&
                            reader.Read())
                        {
                            media.ContentType = reader.GetString();
                            continue;
                        }

                        //
                        // sha256
                        if (pn.IsEqual(SnapshotJsonFields.Sha256) &&
                            reader.Read())
                        {
                            media.Sha256Hash = DecodeBase64Url(reader.GetString());
                            continue;
                        }
                    }

                    if (reader.TokenType == JsonTokenType.EndObject)
                    {
                        break;
                    }
                }

                snapshot.Media = media;
                return;
            }
        }

        private static SnapshotStatus ParseStatus(string value) => value switch
        {
            "provisioning" => SnapshotStatus.Provisioning,
            "ready" => SnapshotStatus.Ready,
            "archived" => SnapshotStatus.Archived,
            "failed" => SnapshotStatus.Failed,
            _ => SnapshotStatus.Provisioning
        };

        private static CompositionType ParseCompositionType(string value) => value switch
        {
            "key" => CompositionType.Key,
            "key_label" => CompositionType.KeyLabel,
            _ => CompositionType.Key
        };

        private static byte[] DecodeBase64Url(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            string base64 = value.Replace('-', '+').Replace('_', '/');

            int pad = base64.Length % 4;

            if (pad > 0)
            {
                base64 = base64 + new string('=', 4 - pad);
            }

            try
            {
                return Convert.FromBase64String(base64);
            }
            catch
            {
                return null;
            }
        }
    }
}
