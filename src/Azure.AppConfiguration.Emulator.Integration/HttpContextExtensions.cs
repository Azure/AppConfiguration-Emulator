// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.AspNetCore.Http;
using System;

namespace Azure.AppConfiguration.Emulator.Integration
{
    internal static class HttpContextExtensions
    {
        public static void SetRetryAfter(this HttpResponse response, TimeSpan retryAfter)
        {
            string value = Math.Max(1, (long)retryAfter.TotalMilliseconds).ToString();

            response.Headers[HttpHeaders.RetryAfterMs] = value;
        }
    }
}
