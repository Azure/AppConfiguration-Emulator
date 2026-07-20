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
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.Service.Formatters.Serializer
{
    [ApiVersion(ApiVersions.V26_04)]
    class KvJsonOutputSerializer2 : IOuputSerializer<KeyValue>
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

            await jw.WriteV2Async(kv, fields);
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
