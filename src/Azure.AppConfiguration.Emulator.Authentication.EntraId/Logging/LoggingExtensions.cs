using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Azure.AppConfiguration.Emulator.Authentication.EntraId
{
    static class LoggingExtensions
    {
        public static Task<HttpResponseMessage> WithLog(
            this Task<HttpResponseMessage> task,
            ILogger logger,
            HttpRequestMessage httpRequest)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            Stopwatch sw = Stopwatch.StartNew();

            return task.ContinueWith(
                t =>
                {
                    if (t.Status == TaskStatus.RanToCompletion)
                    {
                        logger.Log(t.Result, sw.Elapsed);
                    }
                    else
                    {
                        if (httpRequest != null)
                        {
                            logger.LogWarning(httpRequest, sw.Elapsed);
                        }

                        HandleUncompleted(t, logger, sw.Elapsed);
                    }

                    return t.Result;
                },
                TaskContinuationOptions.ExecuteSynchronously);
        }

        public static void LogImpersonatingClient(this ILogger logger, string appId)
        {
            if (!logger.IsEnabled(LogLevel.Information))
            {
                return;
            }

            logger.Log(LogLevel.Information, 0, new KeyValuePair<string, string>(LoggingAttributes.AppId, appId), null, null);
        }

        public static Task<T> WithLogError<T>(this Task<T> task, ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            Stopwatch sw = Stopwatch.StartNew();

            return task.ContinueWith(
                t =>
                {
                    if (t.Status != TaskStatus.RanToCompletion)
                    {
                        HandleUncompleted(t, logger, sw.Elapsed);
                    }

                    return t.Result;
                },
                TaskContinuationOptions.ExecuteSynchronously);
        }

        private static void Log(this ILogger logger, HttpResponseMessage response, TimeSpan duration)
        {
            if (!logger.IsEnabled(LogLevel.Information))
            {
                return;
            }

            var logEntry = new List<object>()
            {
                new KeyValuePair<string, string>(LoggingAttributes.Method, response.RequestMessage.Method.ToString()),
                new KeyValuePair<string, string>(LoggingAttributes.RequestUri, response.RequestMessage.RequestUri.ToString()),
                new KeyValuePair<string, long>(LoggingAttributes.StatusCode, (int)response.StatusCode),
                new KeyValuePair<string, long>(LoggingAttributes.Duration, (long) duration.TotalMilliseconds),
                new KeyValuePair<string, string>(LoggingAttributes.RequestId, response.Headers.GetOrDefault("x-ms-request-id"))
            };

            if (!response.IsSuccessStatusCode)
            {
                //
                // Log the response content
                logEntry.Add(new KeyValuePair<string, string>(LoggingAttributes.Reason, response.ReasonPhrase));

                try
                {
                    logEntry.Add(new KeyValuePair<string, string>(LoggingAttributes.Message, response.Content.ReadAsStringAsync().GetAwaiter().GetResult()));
                }
                catch (HttpRequestException)
                {
                }
            }

            logger.Log(LogLevel.Information, 0, logEntry, null, null);
        }

        private static void LogWarning(this ILogger logger, HttpRequestMessage request, TimeSpan duration)
        {
            Debug.Assert(request != null);

            if (!logger.IsEnabled(LogLevel.Warning))
            {
                return;
            }

            object[] logEntry = new object[]
            {
                new KeyValuePair<string, string>(LoggingAttributes.Method, request.Method.ToString()),
                new KeyValuePair<string, string>(LoggingAttributes.RequestUri, request.RequestUri.ToString()),
                new KeyValuePair<string, long>(LoggingAttributes.Duration, (long) duration.TotalMilliseconds)
            };

            logger.Log(LogLevel.Warning, 0, logEntry, null, null);
        }

        private static void LogWarning(this ILogger logger, string message, TimeSpan duration)
        {
            if (!logger.IsEnabled(LogLevel.Warning))
            {
                return;
            }

            object[] logEntry =
            {
                new KeyValuePair<string, string>(LoggingAttributes.Message, message),
                new KeyValuePair<string, long>(LoggingAttributes.Duration, (long) duration.TotalMilliseconds)
            };

            logger.Log(LogLevel.Warning, 0, logEntry, null, null);
        }

        private static string GetOrDefault(this HttpHeaders headers, string name)
        {
            if (headers.TryGetValues(name, out var values))
            {
                return values.FirstOrDefault();
            }

            return default;
        }

        private static void HandleUncompleted(Task task, ILogger logger, TimeSpan duration)
        {
            //
            // Handle Operation Canceled
            if (task.IsCanceled)
            {
                logger.LogWarning("Operation Canceled", duration);

                //
                // Throws OperationCanceledException
                // The caller usually expects OperationCanceledException directly, instead of wrapped in AgregateException
                task.GetAwaiter().GetResult();
            }

            //
            // Handle Exception
            if (task.IsFaulted)
            {
                task.Exception.Handle(e =>
                {
                    logger.LogError(e.Message, duration);

                    //
                    // Propagates the original exception
                    // The caller usually expects specific exception directly, instead of wrapped in AgregateException
                    throw e;
                });
            }
        }

        private static void LogError(this ILogger logger, string message, TimeSpan duration)
        {
            if (!logger.IsEnabled(LogLevel.Error))
            {
                return;
            }

            object[] logEntry =
            {
                new KeyValuePair<string, string>(LoggingAttributes.Message, message),
                new KeyValuePair<string, long>(LoggingAttributes.Duration, (long) duration.TotalMilliseconds)
            };

            logger.Log(LogLevel.Error, 0, logEntry, null, null);
        }
    }
}
