using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
using System;

namespace Microsoft.AppConfig.Service.Authentication
{
    internal static class HttpRequestExtensions
    {
        public static string GetTarget(this HttpRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            IHttpRequestFeature requestFeature = request.HttpContext.Features.Get<IHttpRequestFeature>();

            if (requestFeature == null)
            {
                throw new InvalidOperationException("Missing IHttpRequestFeature instance");
            }

            //
            // RawTarget is empty in test server mode
            // fall back to best effort
            return string.IsNullOrEmpty(requestFeature.RawTarget) ? new Uri(request.GetEncodedUrl()).PathAndQuery : requestFeature.RawTarget;
        }
    }
}
