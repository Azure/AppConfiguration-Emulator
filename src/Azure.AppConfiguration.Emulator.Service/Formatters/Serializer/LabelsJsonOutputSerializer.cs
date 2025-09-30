// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using Azure.AppConfiguration.Emulator.Service.Formatters.Json;
using Azure.AppConfiguration.Emulator.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.Service.Formatters.Serializer
{
    [ApiVersion(ApiVersions.V1)]
    [ApiVersion(ApiVersions.V22_11_preview)]
    [ApiVersion(ApiVersions.V23_05_preview)]
    [ApiVersion(ApiVersions.V23_10)]
    [ApiVersion(ApiVersions.V23_11)]
    [ApiVersion(ApiVersions.V24_09)]
    class LabelsJsonOutputSerializer : IOuputSerializer<IEnumerable<Label>>
    {
        public async Task WriteContent(JsonWriter jw, IEnumerable<Label> items, long fields)
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

            //
            // items
            await jw.WritePropertyNameAsync("items");

            await jw.WriteStartArrayAsync();

            foreach (Label l in items)
            {
                await jw.WriteAsync(l, fields);
            }

            await jw.WriteEndArrayAsync();

            //
            // Pagination
            var page = items as IPage;

            if (page != null)
            {
                await jw.WritePaginationAsync(page);
            }

            await jw.WriteEndObjectAsync();
        }

        public void WriteResponseHeaders(HttpResponse response, IEnumerable<Label> obj) { }
    }
}
