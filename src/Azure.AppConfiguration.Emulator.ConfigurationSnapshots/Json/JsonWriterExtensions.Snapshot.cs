// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Text.Json;
using Azure.AppConfiguration.Emulator.ConfigurationSettings;

namespace Azure.AppConfiguration.Emulator.ConfigurationSnapshots
{
    internal static class Utf8JsonWriterExtensionsSnapshot
    {
        public static void WriteSnapshot(this Utf8JsonWriter writer, Snapshot snapshot)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            writer.WriteStartObject();

            //
            // id
            if (snapshot.Id != null)
            {
                writer.WriteString(SnapshotJsonFields.Id, snapshot.Id);
            }

            //
            // name
            if (snapshot.Name != null)
            {
                writer.WriteString(SnapshotJsonFields.Name, snapshot.Name);
            }

            //
            // etag
            if (snapshot.Etag != null)
            {
                writer.WriteString(SnapshotJsonFields.Etag, snapshot.Etag);
            }

            //
            // status
            writer.WriteString(SnapshotJsonFields.Status, ToStatus(snapshot.Status));

            //
            // status_code
            writer.WriteNumber(SnapshotJsonFields.StatusCode, snapshot.StatusCode);

            //
            // composition_type
            writer.WriteString(SnapshotJsonFields.CompositionType, ToCompositionType(snapshot.CompositionType));

            //
            // retention_period_seconds
            writer.WriteNumber(SnapshotJsonFields.RetentionPeriodSeconds, (long)snapshot.RetentionPeriod.TotalSeconds);

            //
            // created
            writer.WriteNumber(SnapshotJsonFields.Created, snapshot.Created.ToUnixTimeSeconds());

            //
            // last_modified
            writer.WriteNumber(SnapshotJsonFields.LastModified, snapshot.LastModified.ToUnixTimeSeconds());

            //
            // expires
            if (snapshot.Expires.HasValue)
            {
                writer.WriteNumber(SnapshotJsonFields.Expires, snapshot.Expires.Value.ToUnixTimeSeconds());
            }

            //
            // items_count
            writer.WriteNumber(SnapshotJsonFields.ItemsCount, snapshot.ItemCount);

            //
            // size_bytes
            writer.WriteNumber(SnapshotJsonFields.SizeBytes, snapshot.Size);

            //
            // tags
            if (snapshot.Tags != null)
            {
                writer.WriteDictionary(SnapshotJsonFields.Tags, snapshot.Tags);
            }

            //
            // filters
            if (snapshot.Filters != null)
            {
                writer.WriteStartArray(SnapshotJsonFields.Filters);
                foreach (KeyValueFilter f in snapshot.Filters)
                {
                    if (f != null)
                    {
                        writer.WriteStartObject();
                        if (f.Key != null)
                        {
                            writer.WriteString(SnapshotJsonFields.Key, f.Key);
                        }

                        if (f.Label != null)
                        {
                            writer.WriteString(SnapshotJsonFields.Label, f.Label);
                        }

                        writer.WriteEndObject();
                    }
                }

                writer.WriteEndArray();
            }

            //
            // media
            if (snapshot.Media != null)
            {
                writer.WriteStartObject(SnapshotJsonFields.Media);

                if (snapshot.Media.Category != null)
                {
                    writer.WriteString(SnapshotJsonFields.Category, snapshot.Media.Category);
                }

                if (snapshot.Media.Name != null)
                {
                    writer.WriteString(SnapshotJsonFields.MediaName, snapshot.Media.Name);
                }

                writer.WriteNumber(SnapshotJsonFields.MediaSize, snapshot.Media.Size);

                if (snapshot.Media.Etag != null)
                {
                    writer.WriteString(SnapshotJsonFields.Etag, snapshot.Media.Etag);
                }

                if (snapshot.Media.ContentType != null)
                {
                    writer.WriteString(SnapshotJsonFields.ContentType, snapshot.Media.ContentType);
                }

                if (snapshot.Media.Sha256Hash != null)
                {
                    writer.WriteString(SnapshotJsonFields.Sha256, Base64UrlEncoding.Encode(snapshot.Media.Sha256Hash));
                }

                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }

        private static string ToStatus(SnapshotStatus status) => status switch
        {
            SnapshotStatus.Provisioning => "provisioning",
            SnapshotStatus.Ready => "ready",
            SnapshotStatus.Archived => "archived",
            SnapshotStatus.Failed => "failed",
            _ => "provisioning"
        };

        private static string ToCompositionType(CompositionType type) => type switch
        {
            CompositionType.Key => "key",
            CompositionType.KeyLabel => "key_label",
            _ => "key"
        };
    }
}
