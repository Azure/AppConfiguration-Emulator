// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using System.Text.Json;

namespace Azure.AppConfiguration.Emulator.ConfigurationSettings
{
    public static class ReadOnlySpanExtensions
    {
        public static bool IsEqual(this ReadOnlySpan<byte> span, JsonEncodedText value)
        {
            return span.SequenceEqual(value.EncodedUtf8Bytes);
        }
    }
}
