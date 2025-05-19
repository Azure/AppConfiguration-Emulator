// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Azure.AppConfiguration.Emulator.ConfigurationSettings
{
    public static class JsonReaderExtensions
    {
        public static bool IsTokenNull(this ref Utf8JsonReader reader)
        {
            return reader.TokenType == JsonTokenType.Null;
        }

        public static Dictionary<string, string> ReadDictionary(this ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new InvalidOperationException(reader.TokenType.ToString());
            }

            var result = new Dictionary<string, string>();

            while (reader.Read() &&
                    reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType != JsonTokenType.PropertyName ||
                    reader.HasValueSequence)
                {
                    continue;
                }

                string propertyName = reader.GetString();

                if (reader.Read())
                {
                    result[propertyName] = reader.GetString();
                }
            }

            return result;
        }

        public static IEnumerable<string> ReadStringArray(this ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new InvalidOperationException(reader.TokenType.ToString());
            }

            var result = new List<string>();

            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                result.Add(reader.GetString());
            }

            return result;
        }

        public static void SkipObject(this ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.EndObject ||
                reader.TokenType == JsonTokenType.EndArray)
            {
                return;
            }

            int depth = 1;

            while (depth > 0 && reader.Read())
            {
                if (reader.TokenType == JsonTokenType.StartObject ||
                    reader.TokenType == JsonTokenType.StartArray)
                {
                    ++depth;
                    continue;
                }

                if (reader.TokenType == JsonTokenType.EndObject ||
                    reader.TokenType == JsonTokenType.EndArray)
                {
                    --depth;
                    continue;
                }
            }
        }

        public static bool TrySkipTo(this ref Utf8JsonReader reader, JsonEncodedText propertyName)
        {
            int depth = 1;

            while (depth > 0 && reader.Read())
            {
                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    ++depth;

                    continue;
                }

                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    --depth;

                    continue;
                }

                if (depth == 1 &&
                    reader.TokenType == JsonTokenType.PropertyName &&
                    !reader.HasValueSequence &&
                    reader.ValueSpan.IsEqual(propertyName))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
