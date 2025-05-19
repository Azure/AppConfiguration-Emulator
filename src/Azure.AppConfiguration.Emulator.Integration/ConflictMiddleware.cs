// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using Azure.AppConfiguration.Emulator.Diagnostics;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.Integration
{
    /// <summary>
    /// Handles operation conflict.
    /// It's result of race conditions. Can be retried.
    /// </summary>
    class ConflictMiddleware
    {
        private const string ReasonConflict = "Conflict";
        private static readonly TimeSpan RetryTimeout = TimeSpan.FromMilliseconds(500);

        private readonly RequestDelegate _next;

        public ConflictMiddleware(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (ConflictException)
            {
                HandleConflict(context.Response);
            }
        }

        private static void HandleConflict(HttpResponse response)
        {
            if (response.HasStarted)
            {
                return;
            }

            response.StatusCode = HttpStatusCodes.Status429TooManyRequests;

            //
            // Set retry header
            response.SetRetryAfter(RetryTimeout);

            //
            // Reason
            response.HttpContext.SetStatusReason(ReasonConflict);
        }
    }
}
