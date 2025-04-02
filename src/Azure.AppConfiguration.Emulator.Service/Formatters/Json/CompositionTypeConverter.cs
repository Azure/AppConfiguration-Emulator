// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.AppConfiguration.Emulator.ConfigurationSnapshots;
using Newtonsoft.Json;
using System;

namespace Azure.AppConfiguration.Emulator.Service.Formatters.Json
{
    class CompositionTypeConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(CompositionType?);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            string value = null;

            if (reader.TokenType == JsonToken.String)
            {
                value = (string)reader.Value;
            }

            switch (value)
            {
                case "key":

                    return CompositionType.Key;

                case "key_label":

                    return CompositionType.KeyLabel;

                case null:

                    return null;

                default:

                    throw new JsonException("invalid composition type.");
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
