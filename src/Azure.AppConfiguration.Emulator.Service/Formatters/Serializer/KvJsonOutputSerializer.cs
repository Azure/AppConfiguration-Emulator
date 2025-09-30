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
using System.Globalization;
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
    class KvJsonOutputSerializer : IOuputSerializer<KeyValue>
    {
        public async Task WriteContent(JsonWriter jw, KeyValue kv, long fields)
        {
            if (jw == null)
            {
                throw new ArgumentNullException(nameof(jw));
            }

            if (kv == null)
            {
                throw new ArgumentNullException(nameof(kv));
            }

            await jw.WriteAsync(kv, fields);
        }

        public void WriteResponseHeaders(HttpResponse response, KeyValue obj)
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
            if (!string.IsNullOrEmpty(obj.Etag))
            {
                response.Headers.ETag = new EntityTagHeaderValue($"\"{obj.Etag}\"").ToString();

            }

            //
            // LastModified
            if (obj.Timestamp != default)
            {
                response.Headers.LastModified = obj.Timestamp.ToString(DateTimeFormatInfo.InvariantInfo.RFC1123Pattern, CultureInfo.InvariantCulture);
            }
        }
    }
}
