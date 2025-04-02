// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Azure.AppConfiguration.Emulator.Diagnostics
{
    static class StringExtensions
    {
        public static string Truncate(this string str, int maxLength)
        {
            if (maxLength < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxLength));
            }

            if (maxLength == 0)
            {
                return string.Empty;
            }

            return str.Length > maxLength ? str.Substring(0, maxLength) : str;
        }
    }
}
