using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Linq;
using System.Security.Claims;

namespace Azure.AppConfiguration.Emulator.Diagnostics
{
    public static class HttpContextExtensions
    {
        private static object _statusReason = new object();

        public static string GetClientIpAddress(this HttpContext context)
        {
            if (context.Request.Headers.TryGetValue(HeaderNames.XForwardedFor, out StringValues xForwardedFor))
            {
                return xForwardedFor;
            }

            return context.Connection.RemoteIpAddress.ToString();
        }

        public static bool IsAuthenticated(this ClaimsPrincipal claimsPrincipal)
        {
            return claimsPrincipal.Claims.Any(c => c.Type == ClaimTypes.Sid);
        }

        public static long GetBytesReceived(this HttpRequest request)
        {
            long bytesReceived = 0;

            //
            // scheme
            if (request.Scheme != null)
            {
                bytesReceived += request.Scheme.Length;
            }

            //
            // host
            if (request.Host.HasValue)
            {
                bytesReceived += request.Host.Value.Length;
            }

            //
            // path
            if (request.PathBase.HasValue)
            {
                bytesReceived += request.PathBase.Value.Length;
            }

            if (request.Path.HasValue)
            {
                bytesReceived += request.Path.Value.Length;
            }

            //
            // query string
            if (request.QueryString.HasValue)
            {
                bytesReceived += request.QueryString.Value.Length;
            }

            //
            // headers
            if (request.Headers != null)
            {
                foreach (var header in request.Headers)
                {
                    bytesReceived += header.Key.Length + header.Value.Sum(v => v.Length);
                }
            }

            //
            // Content-Length
            bytesReceived += request.ContentLength ?? 0;

            return bytesReceived;
        }

        public static long GetBytesSent(this HttpResponse response)
        {
            long bytesSent = 0;

            //
            // headers
            if (response.Headers != null)
            {
                foreach (var header in response.Headers)
                {
                    bytesSent += header.Key.Length + header.Value.Sum(v => v.Length);
                }
            }

            //
            // body
            if (response.Body != null)
            {
                bytesSent += response.Body.Length;
            }

            return bytesSent;
        }

        public static string GetUserAgent(this HttpRequest request)
        {
            if (request.Headers != null)
            {
                StringValues value;

                //
                // x-ms-useragent
                if (request.Headers.TryGetValue(HeaderNames.MsUserAgent, out value))
                {
                    return value.ToString();
                }

                //
                // User-Agent
                if (request.Headers.TryGetValue(HeaderNames.UserAgent, out value))
                {
                    return value.ToString();
                }
            }

            return null;
        }

        public static string GetSSLCipher(this HttpRequest request)
        {
            if (request.Headers != null)
            {
                StringValues value;

                //
                // ssl-cipher
                if (request.Headers.TryGetValue(HeaderNames.SSLCipher, out value))
                {
                    return value.ToString();
                }
            }

            return null;
        }

        public static string GetSSLProtocol(this HttpRequest request)
        {
            if (request.Headers != null)
            {
                StringValues value;

                //
                // ssl-protocol
                if (request.Headers.TryGetValue(HeaderNames.SSLProtocol, out value))
                {
                    return value.ToString();
                }
            }

            return null;
        }

        public static string GetStatusReason(this HttpContext context)
        {
            if (context.Items.TryGetValue(_statusReason, out object reason))
            {
                return (string)reason;
            }

            return null;
        }

        public static void SetStatusReason(this HttpContext context, string reason)
        {
            context.Items[_statusReason] = reason;
        }
    }
}
