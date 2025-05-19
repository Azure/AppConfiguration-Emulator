// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.IO;

namespace Azure.AppConfiguration.Emulator.ConfigurationSettings
{
    internal static class StreamExtensions
    {
        private static readonly byte[] _delimiter = new byte[] { (byte)'\r', (byte)'\n' };

        public static void WriteDelimiter(this Stream stream)
        {
            stream.Write(_delimiter);
        }
    }
}
