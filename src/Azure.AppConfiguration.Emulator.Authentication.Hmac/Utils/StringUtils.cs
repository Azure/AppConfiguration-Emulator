// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.AppConfig.Service.Security
{
    static class StringUtils
    {
        public static bool TryConvertFromBase64String(string base64Encoded, out byte[] result)
        {
            result = null;

            if (!string.IsNullOrEmpty(base64Encoded))
            {
                try
                {
                    result = Convert.FromBase64String(base64Encoded);
                }
                catch (FormatException)
                {
                    // Ignore format errors
                }
            }

            return result != null;
        }
    }
}
