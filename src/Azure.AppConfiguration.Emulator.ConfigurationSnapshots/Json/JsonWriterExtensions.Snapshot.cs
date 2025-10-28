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
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

            writer.WriteStartObject();

            writer.WriteString(SnapshotJsonFields.Id, snapshot.Id);
            writer.WriteString(SnapshotJsonFields.Name, snapshot.Name);
            if (snapshot.Etag != null)
            {
                writer.WriteString(SnapshotJsonFields.Etag, snapshot.Etag);
            }

            writer.WriteString(SnapshotJsonFields.Status, ToStatus(snapshot.Status));
            writer.WriteNumber(SnapshotJsonFields.StatusCode, snapshot.StatusCode);
            writer.WriteString(SnapshotJsonFields.CompositionType, ToCompositionType(snapshot.CompositionType));
            writer.WriteNumber(SnapshotJsonFields.RetentionPeriodSeconds, (long)snapshot.RetentionPeriod.TotalSeconds);
            writer.WritePropertyName(SnapshotJsonFields.Created);
            writer.WriteStringValue(snapshot.Created);
            writer.WritePropertyName(SnapshotJsonFields.LastModified);
            writer.WriteStringValue(snapshot.LastModified);
            if (snapshot.Expires.HasValue)
            {
                writer.WritePropertyName(SnapshotJsonFields.Expires);
                writer.WriteStringValue(snapshot.Expires.Value);
            }

            writer.WriteNumber(SnapshotJsonFields.ItemsCount, snapshot.ItemCount);
            writer.WriteNumber(SnapshotJsonFields.SizeBytes, snapshot.Size);

            if (snapshot.Tags != null)
            {
                writer.WriteStartObject(SnapshotJsonFields.Tags);
                foreach (var t in snapshot.Tags)
                {
                    writer.WriteString(t.Key, t.Value);
                }

                writer.WriteEndObject();
            }

            if (snapshot.Filters != null)
            {
                writer.WriteStartArray(SnapshotJsonFields.Filters);
                foreach (var f in snapshot.Filters)
                {
                    writer.WriteStartObject();
                    writer.WriteString(SnapshotJsonFields.Key, f.Key);
                    writer.WriteString(SnapshotJsonFields.Label, f.Label);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
            }

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
