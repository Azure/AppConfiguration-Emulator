// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.AppConfiguration.Emulator.Diagnostics;
using Microsoft.AspNetCore.Http;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using StatusCodes = Microsoft.AspNetCore.Http.StatusCodes;

namespace Azure.AppConfiguration.Emulator.Integration
{
    /// <summary>
    /// Handles when a subsystem can't respond at the moment, but is OK to retry later
    /// </summary>
    class ServiceUnavailableMiddleware
    {
        private const string OperationCanceled = "OperationCanceled";
        private static readonly TimeSpan RetryTimeout = TimeSpan.FromMilliseconds(500);

        private readonly RequestDelegate _next;

        public ServiceUnavailableMiddleware(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task Invoke(HttpContext context)
        {
            TimeSpan retryAfter = RetryTimeout;
            string statusReason = null;

            try
            {
                await _next(context);

                return;
            }
            catch (TimeoutException)
            {
                statusReason = OperationCanceled;
            }

            SetServiceUnavailable(context.Response, retryAfter, statusReason);
        }

        private static void SetServiceUnavailable(HttpResponse response, TimeSpan retryAfter, string statusReason)
        {
            Debug.Assert(response != null);

            if (response.HasStarted)
            {
                return;
            }

            response.Clear();

            //
            // Status code
            response.StatusCode = StatusCodes.Status503ServiceUnavailable;

            //
            // Set retry header
            response.SetRetryAfter(retryAfter);

            //
            // Reason
            if (!string.IsNullOrEmpty(statusReason))
            {
                response.HttpContext.SetStatusReason(statusReason);
            }
        }
    }
}
