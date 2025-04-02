using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.Integration
{
    /// <summary>
    /// Handles server errors
    /// </summary>
    class InternalServerErrorMiddleware
    {
        private const string _loggerCategory = "Microsoft.AppConfig.Service.ServerError";
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        public InternalServerErrorMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
        {
            _next = next;
            _logger = loggerFactory?.CreateLogger(_loggerCategory) ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (InvalidDataException e)
            {
                await HandleInvalidDataException(context, e);
                _logger.LogError(e.Message);
            }
            catch (BadHttpRequestException e)
            {
                _logger.LogError(e, "Bad HttpRequest");
                throw;
            }
            catch (Exception e)
            {
                if (!context.Response.HasStarted)
                {
                    context.Response.Clear();
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                }

                _logger.LogError(e, "Unexpected Exception");
            }
        }

        private static Task HandleInvalidDataException(HttpContext ctx, InvalidDataException e)
        {
            HttpResponse response = ctx.Response;

            if (response.StatusCode >= 400 || response.HasStarted)
            {
                return Task.CompletedTask;
            }

            response.StatusCode = StatusCodes.Status500InternalServerError;

            RouteData routeData = ctx.GetRouteData();

            if (routeData != null)
            {
                ObjectResult result = new ObjectResult(new
                {
                    type = ErrorType.ServerError,
                    title = "Internal Server Error",
                    detail = e.Message,
                    status = response.StatusCode
                });

                return result.ExecuteResultAsync(new ActionContext(ctx, routeData, new ActionDescriptor()));
            }

            return Task.CompletedTask;
        }
    }
}
