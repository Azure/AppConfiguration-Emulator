// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.AppConfiguration.Emulator.ConfigurationSnapshots;
using Azure.AppConfiguration.Emulator.Service.Formatters.Json;
using Azure.AppConfiguration.Emulator.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.Service.Formatters.Serializer
{
    [ApiVersion(ApiVersions.V22_11_preview)]
    [ApiVersion(ApiVersions.V23_05_preview)]
    [ApiVersion(ApiVersions.V23_10)]
    class SnapshotJsonOutputSerializer : IOuputSerializer<Snapshot>
    {
        public async Task WriteContent(JsonWriter jw, Snapshot snapshot, long fields)
        {
            if (jw == null)
            {
                throw new ArgumentNullException(nameof(jw));
            }

            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            await jw.WriteAsync(snapshot, fields);
        }

        public void WriteResponseHeaders(HttpResponse response, Snapshot snapshot)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            //
            // Etag
            if (!string.IsNullOrEmpty(snapshot.Etag))
            {
                response.Headers.ETag = new EntityTagHeaderValue($"\"{snapshot.Etag}\"").ToString();

            }

            //
            // LastModified
            if (snapshot.LastModified != default)
            {
                response.Headers.LastModified = snapshot.LastModified.ToString(DateTimeFormatInfo.InvariantInfo.RFC1123Pattern, CultureInfo.InvariantCulture);
            }

            //
            // items link
            response.Headers.Append(
               HeaderNames.Link,
               $"</kv?snapshot={Uri.EscapeDataString(snapshot.Name)}&api-version={response.HttpContext.GetRequestedApiVersion()}>; rel=\"items\"");
        }
    }
}
