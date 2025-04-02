// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.Authentication.EntraId
{
    static class Retry
    {
        private static readonly TimeSpan MaxRetryTimeout = TimeSpan.FromMilliseconds(5000);
        private static readonly TimeSpan DefaultRetryTimeout = TimeSpan.FromMilliseconds(500);

        public static async Task<HttpResponseMessage> Invoke(Func<Task<HttpResponseMessage>> operation, CancellationToken cancellationToken)
        {
            int retries = 0;
            TimeSpan? retryAfter;

            do
            {
                ++retries;
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    HttpResponseMessage response = await operation().ConfigureAwait(false);

                    if (response.IsSuccessStatusCode)
                    {
                        return response;
                    }

                    retryAfter = response.GetRetryAfter();

                    if (retryAfter == null)
                    {
                        return response;
                    }

                    //
                    // Cleanup
                    response.Dispose();
                }
                catch (TimeoutException)
                {
                    retryAfter = TimeSpan.Zero;
                }
                catch (AggregateException e) when (e.CanRetry())
                {
                    retryAfter = DefaultRetryTimeout;
                }
                catch (HttpRequestException e) when (e.CanRetry())
                {
                    retryAfter = DefaultRetryTimeout;
                }

                if (retryAfter.Value > TimeSpan.Zero)
                {
                    await Task.Delay(retryAfter.Value.BackOff(MaxRetryTimeout, retries), cancellationToken);
                }
            }
            while (true);
        }

        public static async Task<AuthenticationResult> Invoke(Func<Task<AuthenticationResult>> operation, CancellationToken cancellationToken)
        {
            int retries = 0;

            do
            {
                ++retries;
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    return await operation().ConfigureAwait(false);
                }
                catch (AggregateException e) when (e.CanRetry())
                {
                }
                catch (HttpRequestException e) when (e.CanRetry())
                {
                }
                catch (MsalException e) when (e.CanRetry())
                {
                }

                await Task.Delay(DefaultRetryTimeout.BackOff(MaxRetryTimeout, retries), cancellationToken);
            }
            while (true);
        }

        public static async Task<OpenIdConnectConfiguration> Execute(Func<Task<OpenIdConnectConfiguration>> operation, CancellationToken cancellationToken)
        {
            int retries = 0;

            do
            {
                ++retries;
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    return await operation().ConfigureAwait(false);
                }
                catch (HttpRequestException e) when (e.CanRetry())
                {
                }
                catch (InvalidOperationException)
                {
                }

                await Task.Delay(DefaultRetryTimeout.BackOff(MaxRetryTimeout, retries), cancellationToken);
            }
            while (true);
        }

        private static TimeSpan? GetRetryAfter(this HttpResponseMessage response)
        {
            if (response == null)
            {
                return null;
            }

            switch ((int)response.StatusCode)
            {
                case (int)HttpStatusCode.TooManyRequests:
                case (int)HttpStatusCode.InternalServerError:
                case (int)HttpStatusCode.BadGateway:
                case (int)HttpStatusCode.ServiceUnavailable:
                case (int)HttpStatusCode.GatewayTimeout:
                    if (response.Headers.TryGetValues(HeaderNames.RetryAfter, out var values) &&
                        int.TryParse(values.FirstOrDefault(), out int retryAfter))
                    {
                        return TimeSpan.FromSeconds(retryAfter);
                    }
                    else
                    {
                        return DefaultRetryTimeout;
                    }
                default:
                    break;
            }

            return null;
        }

        private static bool CanRetry(this AggregateException e)
        {
            return e.InnerExceptions.Any(inner =>
            {
                switch (inner)
                {
                    case HttpRequestException http:
                        return http.CanRetry();

                    case MsalException msal:
                        return msal.CanRetry();

                    default:
                        return inner.CanRetry();
                }
            });
        }

        private static bool CanRetry(this HttpRequestException e)
        {
            return e.InnerException.CanRetry();
        }

        private static bool CanRetry(this Exception e)
        {
            return e is SocketException ||        // Network issue
                   e is IOException ||            // Network issue
                   e is AuthenticationException;  // Remote SSL cert validation error
        }

        private static bool CanRetry(this MsalException e)
        {
            //
            // MsalServiceException
            if (e is MsalServiceException se)
            {
                switch (se.StatusCode)
                {
                    case (int)HttpStatusCode.TooManyRequests:
                    case (int)HttpStatusCode.InternalServerError:
                    case (int)HttpStatusCode.BadGateway:
                    case (int)HttpStatusCode.ServiceUnavailable:
                    case (int)HttpStatusCode.GatewayTimeout:
                        return true;

                    default:
                        break;
                }
            }

            //
            // MsalException
            switch (e.ErrorCode)
            {
                case MsalError.ServiceNotAvailable:
                case MsalError.RequestTimeout:
                    return true;

                default:
                    break;
            }

            return false;
        }
    }
}
