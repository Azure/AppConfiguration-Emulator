using Azure.AppConfiguration.Emulator.Service.Http;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.Service.Validators
{
    public class PathValidatorMiddleware
    {
        private readonly RequestDelegate _next;

        public PathValidatorMiddleware(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public Task Invoke(HttpContext context)
        {
            if (!IsValidUri(context.Request.GetTarget()))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;

                return Task.CompletedTask;
            }

            return _next(context);
        }

        private static bool IsValidUri(string uri)
        {
            //
            // Prohibit encoded '%' in request path to avoid double decoding later
            int i = uri.IndexOf("%25");

            if (i >= 0)
            {
                //
                // It's fine if %25 is in query string
                return uri.IndexOf('?', 0, i) > 0;
            }

            return true;
        }
    }
}
