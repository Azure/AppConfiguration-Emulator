using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using Azure.AppConfiguration.Emulator.ConfigurationSnapshots;
using Azure.AppConfiguration.Emulator.Service.LongRunningOperation;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.Service.Formatters.Json
{
    static class JsonWriterExtensions
    {
        public static async Task WriteAsync(this JsonWriter writer, KeyValue kv, long fields)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (kv == null)
            {
                throw new ArgumentNullException(nameof(kv));
            }

            await writer.WriteStartObjectAsync();

            //
            // etag
            if ((fields & (long)KeyValueFields.Etag) == (long)KeyValueFields.Etag)
            {
                await writer.WritePropertyNameAsync("etag");
                await writer.WriteValueAsync(kv.Etag);
            }

            //
            // key
            if ((fields & (long)KeyValueFields.Key) == (long)KeyValueFields.Key)
            {
                await writer.WritePropertyNameAsync("key");
                await writer.WriteValueAsync(kv.Key);
            }

            //
            // label
            if ((fields & (long)KeyValueFields.Label) == (long)KeyValueFields.Label)
            {
                await writer.WritePropertyNameAsync("label");
                await writer.WriteValueAsync(kv.Label);
            }

            //
            // content_type
            if ((fields & (long)KeyValueFields.ContentType) == (long)KeyValueFields.ContentType)
            {
                await writer.WritePropertyNameAsync("content_type");
                await writer.WriteValueAsync(kv.ContentType);
            }

            //
            // value
            if ((fields & (long)KeyValueFields.Value) == (long)KeyValueFields.Value)
            {
                await writer.WritePropertyNameAsync("value");
                await writer.WriteValueAsync(kv.Value);
            }

            //
            // tags
            if ((fields & (long)KeyValueFields.Tags) == (long)KeyValueFields.Tags)
            {
                await writer.WritePropertyNameAsync("tags");

                await writer.WriteStartObjectAsync();

                if (kv.Tags != null)
                {
                    foreach (var t in kv.Tags)
                    {
                        await writer.WritePropertyNameAsync(t.Key);
                        await writer.WriteValueAsync(t.Value);
                    }
                }

                await writer.WriteEndObjectAsync();
            }

            //
            // locked
            if ((fields & (long)KeyValueFields.Locked) == (long)KeyValueFields.Locked)
            {
                await writer.WritePropertyNameAsync("locked");
                await writer.WriteValueAsync(kv.Locked);
            }

            //
            // last_modified
            if ((fields & (long)KeyValueFields.LastModified) == (long)KeyValueFields.LastModified)
            {
                await writer.WritePropertyNameAsync("last_modified");
                await writer.WriteValueAsync(kv.Created);
            }

            await writer.WriteEndObjectAsync();
        }

        public static async Task WriteAsync(
            this JsonWriter writer,
            Snapshot snapshot,
            long fields)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            await writer.WriteStartObjectAsync();

            //
            // etag
            if ((fields & (long)SnapshotFields.Etag) == (long)SnapshotFields.Etag)
            {
                await writer.WritePropertyNameAsync("etag");
                await writer.WriteValueAsync(snapshot.Etag);
            }

            //
            // name
            if ((fields & (long)SnapshotFields.Name) == (long)SnapshotFields.Name)
            {
                await writer.WritePropertyNameAsync("name");
                await writer.WriteValueAsync(snapshot.Name);
            }

            // status
            if ((fields & (long)SnapshotFields.Status) == (long)SnapshotFields.Status)
            {
                await writer.WritePropertyNameAsync("status");
                await writer.WriteValueAsync(GetSnapshotStatus(snapshot.Status));
            }

            //
            // filters
            if ((fields & (long)SnapshotFields.Filters) == (long)SnapshotFields.Filters)
            {
                await writer.WritePropertyNameAsync("filters");

                await writer.WriteStartArrayAsync();

                if (snapshot.Filters != null)
                {
                    foreach (var filter in snapshot.Filters)
                    {
                        await writer.WriteStartObjectAsync();

                        //
                        // key
                        await writer.WritePropertyNameAsync("key");
                        await writer.WriteValueAsync(filter.Key);

                        //
                        // label
                        await writer.WritePropertyNameAsync("label");
                        await writer.WriteValueAsync(filter.Label);

                        await writer.WriteEndObjectAsync();
                    }
                }

                await writer.WriteEndArrayAsync();
            }

            //
            // composition_type
            if ((fields & (long)SnapshotFields.CompositionType) == (long)SnapshotFields.CompositionType)
            {
                await writer.WritePropertyNameAsync("composition_type");
                await writer.WriteValueAsync(GetCompositionType(snapshot.CompositionType));
            }

            //
            // created
            if ((fields & (long)SnapshotFields.Created) == (long)SnapshotFields.Created)
            {
                await writer.WritePropertyNameAsync("created");
                await writer.WriteValueAsync(snapshot.Created);
            }

            //
            // expires
            if ((fields & (long)SnapshotFields.Expires) == (long)SnapshotFields.Expires)
            {
                if (snapshot.Expires.HasValue)
                {
                    await writer.WritePropertyNameAsync("expires");
                    await writer.WriteValueAsync(snapshot.Expires.Value);
                }
            }

            //
            // size
            if ((fields & (long)SnapshotFields.Size) == (long)SnapshotFields.Size)
            {
                await writer.WritePropertyNameAsync("size");
                await writer.WriteValueAsync(snapshot.Size);
            }

            //
            // items_count
            if ((fields & (long)SnapshotFields.ItemsCount) == (long)SnapshotFields.ItemsCount)
            {
                await writer.WritePropertyNameAsync("items_count");
                await writer.WriteValueAsync(snapshot.ItemCount);
            }

            //
            // tags
            if ((fields & (long)SnapshotFields.Tags) == (long)SnapshotFields.Tags)
            {
                await writer.WritePropertyNameAsync("tags");

                await writer.WriteStartObjectAsync();

                if (snapshot.Tags != null)
                {
                    foreach (var t in snapshot.Tags)
                    {
                        await writer.WritePropertyNameAsync(t.Key);
                        await writer.WriteValueAsync(t.Value);
                    }
                }

                await writer.WriteEndObjectAsync();
            }

            //
            // retention_period
            if ((fields & (long)SnapshotFields.RetentionPeriod) == (long)SnapshotFields.RetentionPeriod)
            {
                await writer.WritePropertyNameAsync("retention_period");
                await writer.WriteValueAsync((int)snapshot.RetentionPeriod.TotalSeconds);
            }

            await writer.WriteEndObjectAsync();
        }

        public static async Task WriteV2Async(
            this JsonWriter writer,
            Snapshot snapshot,
            long fields)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            await writer.WriteStartObjectAsync();

            //
            // etag
            if ((fields & (long)SnapshotFields.Etag) == (long)SnapshotFields.Etag)
            {
                await writer.WritePropertyNameAsync("etag");
                await writer.WriteValueAsync(snapshot.Etag);
            }

            //
            // name
            if ((fields & (long)SnapshotFields.Name) == (long)SnapshotFields.Name)
            {
                await writer.WritePropertyNameAsync("name");
                await writer.WriteValueAsync(snapshot.Name);
            }

            // status
            if ((fields & (long)SnapshotFields.Status) == (long)SnapshotFields.Status)
            {
                await writer.WritePropertyNameAsync("status");
                await writer.WriteValueAsync(GetSnapshotStatus(snapshot.Status));
            }

            //
            // filters
            if ((fields & (long)SnapshotFields.Filters) == (long)SnapshotFields.Filters)
            {
                await writer.WritePropertyNameAsync("filters");

                await writer.WriteStartArrayAsync();

                if (snapshot.Filters != null)
                {
                    foreach (var filter in snapshot.Filters)
                    {
                        await writer.WriteStartObjectAsync();

                        //
                        // key
                        await writer.WritePropertyNameAsync("key");
                        await writer.WriteValueAsync(filter.Key);

                        //
                        // label
                        await writer.WritePropertyNameAsync("label");
                        await writer.WriteValueAsync(filter.Label);

                        //
                        // tags
                        await writer.WritePropertyNameAsync("tags");
                        await writer.WriteStartArrayAsync();

                        if (filter.Tags != null)
                        {
                            const char tagFilterSeparator = '=';

                            foreach (KeyValuePair<string, string> tagFilter in filter.Tags)
                            {
                                // escape all reserved characters and '=' character
                                string escapedKey = SearchQuery.Escape(tagFilter.Key, tagFilterSeparator);
                                string escapedValue = SearchQuery.Escape(tagFilter.Value, tagFilterSeparator);

                                await writer.WriteValueAsync($"{escapedKey}{tagFilterSeparator}{escapedValue}");
                            }
                        }

                        await writer.WriteEndArrayAsync();

                        await writer.WriteEndObjectAsync();
                    }
                }

                await writer.WriteEndArrayAsync();
            }

            //
            // composition_type
            if ((fields & (long)SnapshotFields.CompositionType) == (long)SnapshotFields.CompositionType)
            {
                await writer.WritePropertyNameAsync("composition_type");
                await writer.WriteValueAsync(GetCompositionType(snapshot.CompositionType));
            }

            //
            // created
            if ((fields & (long)SnapshotFields.Created) == (long)SnapshotFields.Created)
            {
                await writer.WritePropertyNameAsync("created");
                await writer.WriteValueAsync(snapshot.Created);
            }

            //
            // expires
            if ((fields & (long)SnapshotFields.Expires) == (long)SnapshotFields.Expires)
            {
                if (snapshot.Expires.HasValue)
                {
                    await writer.WritePropertyNameAsync("expires");
                    await writer.WriteValueAsync(snapshot.Expires.Value);
                }
            }

            //
            // size
            if ((fields & (long)SnapshotFields.Size) == (long)SnapshotFields.Size)
            {
                await writer.WritePropertyNameAsync("size");
                await writer.WriteValueAsync(snapshot.Size);
            }

            //
            // items_count
            if ((fields & (long)SnapshotFields.ItemsCount) == (long)SnapshotFields.ItemsCount)
            {
                await writer.WritePropertyNameAsync("items_count");
                await writer.WriteValueAsync(snapshot.ItemCount);
            }

            //
            // tags
            if ((fields & (long)SnapshotFields.Tags) == (long)SnapshotFields.Tags)
            {
                await writer.WritePropertyNameAsync("tags");

                await writer.WriteStartObjectAsync();

                if (snapshot.Tags != null)
                {
                    foreach (var t in snapshot.Tags)
                    {
                        await writer.WritePropertyNameAsync(t.Key);
                        await writer.WriteValueAsync(t.Value);
                    }
                }

                await writer.WriteEndObjectAsync();
            }

            //
            // retention_period
            if ((fields & (long)SnapshotFields.RetentionPeriod) == (long)SnapshotFields.RetentionPeriod)
            {
                await writer.WritePropertyNameAsync("retention_period");
                await writer.WriteValueAsync((int)snapshot.RetentionPeriod.TotalSeconds);
            }

            await writer.WriteEndObjectAsync();
        }

        public static async Task WriteAsync(
           this JsonWriter writer,
           OperationStatus operationStatus)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (operationStatus == null)
            {
                throw new ArgumentNullException(nameof(operationStatus));
            }

            await writer.WriteStartObjectAsync();

            //
            // id
            await writer.WritePropertyNameAsync("id");
            await writer.WriteValueAsync(operationStatus.Id);

            //
            // status
            await writer.WritePropertyNameAsync("status");
            await writer.WriteValueAsync(GetStatus(operationStatus.Status));

            //
            // error
            await writer.WritePropertyNameAsync("error");

            if (operationStatus.Error == null)
            {
                await writer.WriteNullAsync();
            }
            else
            {
                await writer.WriteStartObjectAsync();

                await writer.WritePropertyNameAsync("code");
                await writer.WriteValueAsync(operationStatus.Error.Code);

                await writer.WritePropertyNameAsync("message");
                await writer.WriteValueAsync(operationStatus.Error.Message);

                await writer.WriteEndObjectAsync();
            }

            await writer.WriteEndObjectAsync();
        }

        private static string GetSnapshotStatus(SnapshotStatus status)
        {
            switch (status)
            {
                case SnapshotStatus.Provisioning:
                    return "provisioning";
                case SnapshotStatus.Ready:
                    return "ready";
                case SnapshotStatus.Archived:
                    return "archived";
                case SnapshotStatus.Failed:
                    return "failed";
                default:
                    throw new NotImplementedException();
            }
        }

        public static async Task WriteAsync(this JsonWriter writer, Label label, long fields)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (label == null)
            {
                throw new ArgumentNullException(nameof(label));
            }

            await writer.WriteStartObjectAsync();

            //
            // name
            if ((fields & (long)LabelFields.Name) == (long)LabelFields.Name)
            {
                await writer.WritePropertyNameAsync("name");
                await writer.WriteValueAsync(label.Name);
            }

            await writer.WriteEndObjectAsync();
        }

        public static async Task WriteAsync(this JsonWriter writer, Key key, long fields)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            await writer.WriteStartObjectAsync();

            //
            // name
            if ((fields & (long)KeyFields.Name) == (long)KeyFields.Name)
            {
                await writer.WritePropertyNameAsync("name");
                await writer.WriteValueAsync(key.Name);
            }

            await writer.WriteEndObjectAsync();
        }

        public static async Task WriteEtagAsync(this JsonWriter writer, string etag)
        {
            if (!string.IsNullOrEmpty(etag))
            {
                await writer.WritePropertyNameAsync("etag");
                await writer.WriteValueAsync(etag);
            }
        }

        public static async Task WritePaginationAsync(this JsonWriter writer, IPage page)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (page == null)
            {
                throw new ArgumentNullException(nameof(page));
            }

            if (!string.IsNullOrEmpty(page.NextLink))
            {
                await writer.WritePropertyNameAsync("@nextLink");
                await writer.WriteValueAsync(page.NextLink);
            }
        }

        public static void SetSettings(this JsonWriter writer, JsonSerializerSettings settings)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            writer.DateFormatString = settings.DateFormatString;
            writer.DateTimeZoneHandling = settings.DateTimeZoneHandling;
            writer.DateFormatHandling = settings.DateFormatHandling;
            writer.FloatFormatHandling = settings.FloatFormatHandling;
            writer.Formatting = settings.Formatting;
        }

        private static string GetCompositionType(CompositionType compositionType)
        {
            switch (compositionType)
            {
                case CompositionType.Key:

                    return "key";

                case CompositionType.KeyLabel:

                    return "key_label";

                default:

                    throw new NotImplementedException();
            }
        }

        private static string GetStatus(Status status)
        {
            //
            // String values follow the Azure API guidelines for long running operations:
            // https://github.com/microsoft/api-guidelines/blob/vNext/azure/Guidelines.md#long-running-operations--jobs
            switch (status)
            {
                case Status.Running:

                    return "Running";

                case Status.Succeeded:

                    return "Succeeded";

                case Status.Failed:

                    return "Failed";

                default:

                    throw new NotImplementedException();
            }
        }
    }
}
