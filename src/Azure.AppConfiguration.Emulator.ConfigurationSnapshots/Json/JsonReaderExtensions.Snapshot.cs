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
        public static bool TryReadSnapshot(this ref Utf8JsonReader reader, out Snapshot snapshot)
        {
            snapshot = null;
            int depth = (reader.TokenType == JsonTokenType.StartObject) ? 1 : 0;
            Snapshot s = null;

            while (reader.Read())
            {
                if (depth == 1 && reader.TokenType == JsonTokenType.PropertyName && !reader.HasValueSequence)
                {
                    s ??= new Snapshot();
                    ReadSnapshotProperty(s, reader.ValueSpan, ref reader);
                    continue;
                }

                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    depth++;
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
                snapshot = s;
            }

            return snapshot != null;
        }

        private static void ReadSnapshotProperty(Snapshot snapshot, ReadOnlySpan<byte> name, ref Utf8JsonReader reader)
        {
            if (name.SequenceEqual(SnapshotJsonFields.Id.EncodedUtf8Bytes))
            {
                if (reader.Read() && reader.TokenType == JsonTokenType.String) snapshot.Id = reader.GetString();
                return;
            }

            if (name.SequenceEqual(SnapshotJsonFields.Name.EncodedUtf8Bytes))
            {
                if (reader.Read() && reader.TokenType == JsonTokenType.String) snapshot.Name = reader.GetString();
                return;
            }

            if (name.SequenceEqual(SnapshotJsonFields.Etag.EncodedUtf8Bytes))
            {
                if (reader.Read() && reader.TokenType == JsonTokenType.String) snapshot.Etag = reader.GetString();
                return;
            }

            if (name.SequenceEqual(SnapshotJsonFields.Status.EncodedUtf8Bytes))
            {
                if (reader.Read() && reader.TokenType == JsonTokenType.String) snapshot.Status = ParseStatus(reader.GetString());
                return;
            }

            if (name.SequenceEqual(SnapshotJsonFields.StatusCode.EncodedUtf8Bytes))
            {
                if (reader.Read()) snapshot.StatusCode = reader.GetInt32();
                return;
            }

            if (name.SequenceEqual(SnapshotJsonFields.CompositionType.EncodedUtf8Bytes))
            {
                if (reader.Read() && reader.TokenType == JsonTokenType.String) snapshot.CompositionType = ParseCompositionType(reader.GetString());
                return;
            }

            if (name.SequenceEqual(SnapshotJsonFields.RetentionPeriodSeconds.EncodedUtf8Bytes))
            {
                if (reader.Read()) snapshot.RetentionPeriod = TimeSpan.FromSeconds(reader.GetInt64());
                return;
            }

            if (name.SequenceEqual(SnapshotJsonFields.Created.EncodedUtf8Bytes))
            {
                if (reader.Read()) snapshot.Created = reader.GetDateTimeOffset();
                return;
            }

            if (name.SequenceEqual(SnapshotJsonFields.LastModified.EncodedUtf8Bytes))
            {
                if (reader.Read()) snapshot.LastModified = reader.GetDateTimeOffset();
                return;
            }

            if (name.SequenceEqual(SnapshotJsonFields.Expires.EncodedUtf8Bytes))
            {
                if (reader.Read()) snapshot.Expires = reader.GetDateTimeOffset();
                return;
            }

            if (name.SequenceEqual(SnapshotJsonFields.ItemsCount.EncodedUtf8Bytes))
            {
                if (reader.Read()) snapshot.ItemCount = reader.GetInt64();
                return;
            }

            if (name.SequenceEqual(SnapshotJsonFields.SizeBytes.EncodedUtf8Bytes))
            {
                if (reader.Read()) snapshot.Size = reader.GetInt64();
                return;
            }

            if (name.SequenceEqual(SnapshotJsonFields.Tags.EncodedUtf8Bytes))
            {
                if (reader.Read()) snapshot.Tags = reader.ReadDictionary();
                return;
            }

            if (name.SequenceEqual(SnapshotJsonFields.Filters.EncodedUtf8Bytes))
            {
                if (reader.Read() && reader.TokenType == JsonTokenType.StartArray)
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
                                    if (pn.SequenceEqual(SnapshotJsonFields.Key.EncodedUtf8Bytes))
                                    {
                                        if (reader.Read() && reader.TokenType == JsonTokenType.String) key = reader.GetString();
                                        continue;
                                    }

                                    if (pn.SequenceEqual(SnapshotJsonFields.Label.EncodedUtf8Bytes))
                                    {
                                        if (reader.Read() && reader.TokenType == JsonTokenType.String) label = reader.GetString();
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
                }

                return;
            }

            if (name.SequenceEqual(SnapshotJsonFields.Media.EncodedUtf8Bytes))
            {
                if (reader.Read() && reader.TokenType == JsonTokenType.StartObject)
                {
                    MediaInfo media = new MediaInfo();
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.PropertyName && !reader.HasValueSequence)
                        {
                            ReadOnlySpan<byte> pn = reader.ValueSpan;
                            if (pn.SequenceEqual(SnapshotJsonFields.Category.EncodedUtf8Bytes))
                            {
                                if (reader.Read() && reader.TokenType == JsonTokenType.String) media.Category = reader.GetString();
                                continue;
                            }

                            if (pn.SequenceEqual(SnapshotJsonFields.MediaName.EncodedUtf8Bytes))
                            {
                                if (reader.Read() && reader.TokenType == JsonTokenType.String) media.Name = reader.GetString();
                                continue;
                            }

                            if (pn.SequenceEqual(SnapshotJsonFields.MediaSize.EncodedUtf8Bytes))
                            {
                                if (reader.Read()) media.Size = reader.GetInt64();
                                continue;
                            }

                            if (pn.SequenceEqual(SnapshotJsonFields.Etag.EncodedUtf8Bytes))
                            {
                                if (reader.Read() && reader.TokenType == JsonTokenType.String) media.Etag = reader.GetString();
                                continue;
                            }

                            if (pn.SequenceEqual(SnapshotJsonFields.ContentType.EncodedUtf8Bytes))
                            {
                                if (reader.Read() && reader.TokenType == JsonTokenType.String) media.ContentType = reader.GetString();
                                continue;
                            }

                            if (pn.SequenceEqual(SnapshotJsonFields.Sha256.EncodedUtf8Bytes))
                            {
                                if (reader.Read() && reader.TokenType == JsonTokenType.String) media.Sha256Hash = DecodeBase64Url(reader.GetString());
                                continue;
                            }
                        }

                        if (reader.TokenType == JsonTokenType.EndObject)
                        {
                            break;
                        }
                    }

                    snapshot.Media = media;
                }

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
            if (string.IsNullOrEmpty(value)) return null;
            string base64 = value.Replace('-', '+').Replace('_', '/');
            int pad = base64.Length % 4;
            if (pad > 0) base64 = base64 + new string('=', 4 - pad);
            try { return Convert.FromBase64String(base64); } catch { return null; }
        }
    }
}
