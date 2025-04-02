// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Azure.AppConfiguration.Emulator.Authentication.EntraId
{
    static class Json
    {
        public static readonly MediaTypeWithQualityHeaderValue ApplicationJsonMediaType =
            new MediaTypeWithQualityHeaderValue("application/json")
            {
                CharSet = Encoding.UTF8.WebName
            };

        public static void WriteArray(this Utf8JsonWriter writer, JsonEncodedText name, IEnumerable<string> values)
        {
            writer.WriteStartArray(name);

            foreach (string value in values)
            {
                writer.WriteStringValue(value);
            }

            writer.WriteEndArray();
        }

        public static void WriteArray<T>(this Utf8JsonWriter writer, JsonEncodedText name, IEnumerable<T> values, Action<T> writeItem)
        {
            writer.WriteStartArray(name);

            foreach (T value in values)
            {
                writer.WriteStartObject();

                writeItem(value);

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        public static byte[] Serialize<T>(T value, JsonSerializerOptions options)
        {
            return JsonSerializer.SerializeToUtf8Bytes(value, options);
        }

        public static T Deserialize<T>(byte[] utf8Bytes, JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize<T>(utf8Bytes, options);
        }
    }
}
