using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.Integration
{
    /// <summary>
    /// Handles client request aborted
    /// </summary>
    class RequestAbortedMiddleware
    {
        private readonly RequestDelegate _next;

        public RequestAbortedMiddleware(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                HandleRequestAborted(context.Response);
            }
        }

        private static void HandleRequestAborted(HttpResponse response)
        {
            if (response.HasStarted)
            {
                return;
            }

            //
            // Status code
            // This is usually not returned in a response, but will be logged
            response.StatusCode = HttpStatusCodes.Status499ClientClosedRequest;
        }
    }
}
