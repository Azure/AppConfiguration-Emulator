using Azure.AppConfiguration.Emulator.Tenant;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.Diagnostics
{
    public class DiagnosticsMiddleware
    {
        private const string HttpCategory = "Microsoft.AppConfig.Service.HttpLogging";
        private const string UnobservedTaskExceptionCategory = "Microsoft.AppConfig.Service.UnobservedTaskException";

        struct HttpActivityContext
        {
            public string RequestId { get; set; }
            public string ClientRequestId { get; set; }
            public string CorrelationRequestId { get; set; }
        }

        private readonly RequestDelegate _next;
        private readonly TenantOptions _tenant;
        private readonly HttpLoggingOptions _httpOptions;
        private readonly ILogger _httpLogger;
        private readonly ILogger _unobservedTaskLogger;

        public DiagnosticsMiddleware(
            RequestDelegate next,
            IOptions<TenantOptions> tenant,
            IOptions<HttpLoggingOptions> httpOptions,
            ILoggerFactory loggerFactory)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _tenant = tenant?.Value ?? throw new ArgumentNullException(nameof(tenant));

            ValidateHttpOptions(httpOptions?.Value);
            _httpOptions = httpOptions.Value;

            _httpLogger = loggerFactory?.CreateLogger(HttpCategory) ?? throw new ArgumentNullException(nameof(loggerFactory));

            _unobservedTaskLogger = loggerFactory?.CreateLogger(UnobservedTaskExceptionCategory) ?? throw new ArgumentNullException(nameof(loggerFactory));

            HandleUnobservedTasks();
        }

        /// <summary>
        /// This method should not be async since it sets ScopedContext
        /// Check <see cref="ScopedContext"/> for details
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Invoke(HttpContext context)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            HttpActivityContext activity = GetActivityContext(context);

            //
            // Request size
            int bytesReceived = (int)context.Request.GetBytesReceived();

            //
            // Hook up stream tracker because Response.Body is write-only
            // Will be disposed as the HttpResponse is done (see RegisterForDispose)
            HttpResponse response = context.Response;
            Stream originStream = response.Body;

            var streamTracker = new StreamTracker(originStream);

            response.RegisterForDispose(streamTracker);

            response.Body = streamTracker;

            //
            // Sent request identifications
            context.Response.OnStarting(() =>
            {
                stopwatch.Stop();
                WriteResponseHeaders(context, activity);

                return Task.CompletedTask;
            });

            //
            // Let below lambda capture this variable
            context.Response.OnCompleted(() =>
            {
                int bytesSent = 0;

                //
                // Restore the origin stream
                if (response.Body == streamTracker)
                {
                    bytesSent = (int)response.GetBytesSent();
                    response.Body = originStream;
                }

                //
                // Logging
                _httpLogger.LogHttp(
                    context,
                    stopwatch.Elapsed,
                    activity.RequestId,
                    activity.ClientRequestId,
                    activity.CorrelationRequestId,
                    bytesReceived,
                    bytesSent);

                return Task.CompletedTask;
            });

            //
            // Create and start a HttpRequestActivity
            await _next(context);
        }

        private static void WriteResponseHeaders(HttpContext httpContext, HttpActivityContext activity)
        {
            IHeaderDictionary headers = httpContext.Response.Headers;

            //
            // RequestId
            headers[HeaderNames.RequestId] = activity.RequestId;

            //
            // ClientRequestId
            if (httpContext.Request.Headers.ContainsKey(HeaderNames.ReturnClientRequestId))
            {
                headers[HeaderNames.ClientRequestId] = activity.ClientRequestId;
            }

            //
            // CorrelationRequestId
            headers[HeaderNames.CorrelationRequestId] = activity.CorrelationRequestId;
        }

        private void HandleUnobservedTasks()
        {
            TaskScheduler.UnobservedTaskException += (sender, eventArgs) =>
            {
                eventArgs.SetObserved();
                eventArgs.Exception.Handle(ex =>
                {
                    _unobservedTaskLogger.LogError(ex, "Unobserved task exception.");
                    return true;
                });
            };
        }

        private HttpActivityContext GetActivityContext(HttpContext httpContext)
        {
            IHeaderDictionary requestHeaders = httpContext.Request.Headers;
            string requestId = Guid.NewGuid().ToString();

            var activity = new HttpActivityContext
            {
                RequestId = requestId,
                ClientRequestId = requestHeaders[HeaderNames.ClientRequestId],
                CorrelationRequestId = requestHeaders.ContainsKey(HeaderNames.CorrelationRequestId) ? requestHeaders[HeaderNames.CorrelationRequestId].ToString() : requestId
            };

            return activity;
        }

        private static void ValidateHttpOptions(HttpLoggingOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options.MaxLogValueLength <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options.MaxLogValueLength));
            }

            if (options.MaxUserAgentLength <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options.MaxUserAgentLength));
            }

            if (options.RequestHeaders != null)
            {
                foreach (LogHeaderInfo headerInfo in options.RequestHeaders)
                {
                    if (string.IsNullOrEmpty(headerInfo.HeaderName))
                    {
                        throw new ArgumentNullException(nameof(headerInfo.HeaderName));
                    }

                    if (string.IsNullOrEmpty(headerInfo.LogAttributeName))
                    {
                        throw new ArgumentNullException(nameof(headerInfo.LogAttributeName));
                    }

                    if (headerInfo.MaxLength <= 0)
                    {
                        throw new ArgumentOutOfRangeException(nameof(headerInfo.MaxLength));
                    }

                    if (headerInfo.MaxLength == int.MaxValue)
                    {
                        throw new ArgumentOutOfRangeException(nameof(headerInfo.MaxLength), "Request header max length is not specified.");
                    }
                }
            }

            if (options.ResponseHeaders != null)
            {
                foreach (LogHeaderInfo headerInfo in options.ResponseHeaders)
                {
                    if (string.IsNullOrEmpty(headerInfo.HeaderName))
                    {
                        throw new ArgumentNullException(nameof(headerInfo.HeaderName));
                    }

                    if (string.IsNullOrEmpty(headerInfo.LogAttributeName))
                    {
                        throw new ArgumentNullException(nameof(headerInfo.LogAttributeName));
                    }

                    if (headerInfo.MaxLength <= 0)
                    {
                        throw new ArgumentOutOfRangeException(nameof(headerInfo.MaxLength));
                    }
                }
            }
        }
    }
}
