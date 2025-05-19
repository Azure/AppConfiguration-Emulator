// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Azure.AppConfiguration.Emulator.ConfigurationSettings
{
    public static class JsonWriterExtensions
    {
        public static void WriteDictionary(
            this Utf8JsonWriter writer,
            JsonEncodedText propertyName,
            IEnumerable<KeyValuePair<string, string>> propertyValue)
        {
            if (propertyName.EncodedUtf8Bytes.IsEmpty)
            {
                throw new ArgumentNullException("propertyName");
            }

            if (propertyValue == null)
            {
                writer.WriteNull(propertyName);

                return;
            }

            writer.WriteStartObject(propertyName);

            foreach (KeyValuePair<string, string> item in propertyValue)
            {
                writer.WriteString(item.Key, item.Value);
            }

            writer.WriteEndObject();
        }

        public static void WriteStringArray(
            this Utf8JsonWriter writer,
            JsonEncodedText propertyName,
            IEnumerable<string> array)
        {
            if (propertyName.EncodedUtf8Bytes.IsEmpty)
            {
                throw new ArgumentNullException("propertyName");
            }

            if (array == null)
            {
                writer.WriteNull(propertyName);

                return;
            }

            writer.WriteStartArray(propertyName);

            foreach (string item in array)
            {
                writer.WriteStringValue(item);
            }

            writer.WriteEndArray();
        }
    }
}
