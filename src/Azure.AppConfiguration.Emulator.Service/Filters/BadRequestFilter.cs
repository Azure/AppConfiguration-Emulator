using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using Azure.AppConfiguration.Emulator.Integration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json.Serialization;
using System;
using System.Text;

namespace Azure.AppConfiguration.Emulator.Service
{
    public class BadRequestFilter : ActionFilterAttribute
    {
        private readonly SnakeCaseNamingStrategy _namingStrategy = new SnakeCaseNamingStrategy();

        public override void OnActionExecuted(ActionExecutedContext ctx)
        {
            base.OnActionExecuted(ctx);

            object err;

            HttpResponse response = ctx.HttpContext.Response;

            if (response.StatusCode == StatusCodes.Status422UnprocessableEntity ||
                response.StatusCode == StatusCodes.Status400BadRequest)
            {
                //
                // Handle HTTP 400/422
                string paramName = (ctx.Result as ObjectResult)?.Value as string;

                int indexOfPeriod = paramName.IndexOf('.');

                if (indexOfPeriod >= 0)
                {
                    //
                    // E.g. "Filters[0].Key"
                    StringBuilder sb = new StringBuilder();

                    foreach (string part in paramName.Split('.'))
                    {
                        if (sb.Length > 0)
                        {
                            sb.Append('.');
                        }

                        sb.Append(_namingStrategy.GetPropertyName(part, false));
                    }

                    paramName = sb.ToString();
                }
                else
                {
                    //
                    // E.g. "ContentType"
                    paramName = _namingStrategy.GetPropertyName(paramName, false);
                }

                err = CreateErrorObject(paramName);
            }
            else
            {
                //
                // Handle exceptions
                err = CreateErrorObject(ctx.Exception);
            }

            if (err != null)
            {
                ctx.Exception = null;
                response.StatusCode = StatusCodes.Status400BadRequest;

                ctx.Result = new ObjectResult(err);
            }
        }

        private static object CreateErrorObject(Exception e)
        {
            if (e == null)
            {
                return null;
            }

            switch (e)
            {
                case RequestArgumentException rae:
                    return new
                    {
                        type = ErrorType.InvalidArgument,
                        title = ToErrorTitle(rae.ParamName),
                        name = rae.ParamName,
                        detail = rae.Message ?? string.Empty,
                        status = StatusCodes.Status400BadRequest
                    };
                case SearchQueryException sqe:
                    return new
                    {
                        type = ErrorType.InvalidArgument,
                        title = ToErrorTitle(sqe.ParamName),
                        name = sqe.ParamName,
                        detail = sqe.Message ?? string.Empty,
                        pos = sqe.Position,
                        status = StatusCodes.Status400BadRequest
                    };
                default:
                    break;
            }

            return null;
        }

        private static object CreateErrorObject(string paramName)
        {
            if (string.IsNullOrEmpty(paramName))
            {
                return null;
            }

            return new
            {
                type = ErrorType.InvalidArgument,
                title = ToErrorTitle(paramName),
                name = paramName,
                detail = string.Empty,
                status = StatusCodes.Status400BadRequest
            };
        }

        private static string ToErrorTitle(string paramName)
        {
            return $"Invalid request parameter '{paramName}'";
        }
    }
}
