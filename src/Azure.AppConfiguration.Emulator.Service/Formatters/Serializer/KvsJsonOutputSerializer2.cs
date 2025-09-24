// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using Azure.AppConfiguration.Emulator.Service.Formatters.Json;
using Azure.AppConfiguration.Emulator.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.Service.Formatters.Serializer
{
    [ApiVersion(ApiVersions.V23_05_preview)]
    [ApiVersion(ApiVersions.V23_10)]
    [ApiVersion(ApiVersions.V23_11)]
    [ApiVersion(ApiVersions.V24_09)]
    class KvsJsonOutputSerializer2 : IOuputSerializer<IEnumerable<KeyValue>>
    {
        public async Task WriteContent(JsonWriter jw, IEnumerable<KeyValue> items, long fields)
        {
            if (jw == null)
            {
                throw new ArgumentNullException(nameof(jw));
            }

            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            await jw.WriteStartObjectAsync();

            IPage page = items as IPage;

            //
            // etag
            if (page != null)
            {
                await jw.WriteEtagAsync(page.Etag);
            }

            //
            // items
            await jw.WritePropertyNameAsync("items");

            await jw.WriteStartArrayAsync();

            foreach (KeyValue kv in items)
            {
                await jw.WriteAsync(kv, fields);
            }

            await jw.WriteEndArrayAsync();

            //
            // Pagination
            if (page != null)
            {
                await jw.WritePaginationAsync(page);
            }

            await jw.WriteEndObjectAsync();
        }

        public void WriteResponseHeaders(HttpResponse response, IEnumerable<KeyValue> obj)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            //
            // Etag
            var etag = (IPage)obj;

            if (string.IsNullOrEmpty(etag.Etag))
            {
                return;
            }

            response.Headers.ETag = new EntityTagHeaderValue($"\"{etag.Etag}\"").ToString();
        }
    }
}
