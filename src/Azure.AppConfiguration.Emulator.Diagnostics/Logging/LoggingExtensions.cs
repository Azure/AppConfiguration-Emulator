using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Azure.AppConfiguration.Emulator.Diagnostics
{
    internal static class LoggingExtensions
    {
        public static void LogHttp(
            this ILogger logger,
            HttpContext context,
            TimeSpan duration,
            string requestId,
            string clientRequestId,
            string correlationRequestId,
            int bytesReceived,
            int bytesSent)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!logger.IsEnabled(LogLevel.Information))
            {
                return;
            }

            //
            // TODO:
            //

            var logEntry = new List<object>()
            {
            };

            logger.Log(LogLevel.Information, 0, logEntry, null, null);
        }
    }
}
