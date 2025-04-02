// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using Azure.AppConfiguration.Emulator.Service.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq;
using Headers = Microsoft.Net.Http.Headers;

namespace Azure.AppConfiguration.Emulator.Service
{
    public class RangeFilter : ActionFilterAttribute
    {
        private const string BindingArgName = "range";

        public override void OnActionExecuting(ActionExecutingContext ctx)
        {
            if (ctx.ActionDescriptor.Parameters.Any(p => p.Name == BindingArgName))
            {
                ctx.ActionArguments[BindingArgName] = ctx.HttpContext.Request.GetItemsRange();
            }
        }

        public override void OnActionExecuted(ActionExecutedContext ctx)
        {
            base.OnActionExecuted(ctx);

            HandleRange(ctx);

            WriteHeaders(ctx);
        }

        private void WriteHeaders(ActionExecutedContext ctx)
        {
            object result = (ctx.Result as ObjectResult)?.Value;

            if (result == null)
            {
                return;
            }

            HttpResponse response = ctx.HttpContext.Response;

            var page = result as IPage;

            if (page != null)
            {
                //
                // Accept-Ranges
                response.Headers.Append(Headers.HeaderNames.AcceptRanges, "items");

                if (page.Count != page.TotalItemsCount ||
                    ctx.HttpContext.Request.Headers[Headers.HeaderNames.Range].Count > 0)
                {
                    string range = null;

                    if (page.TotalItemsCount > 0)
                    {
                        range = $"{page.Offset}-{page.Offset + page.Count - 1}/{page.TotalItemsCount}";
                    }
                    else if (page.TotalItemsCount == 0)
                    {
                        range = "*/0";
                    }
                    else
                    {
                        if (page.Count > 0)
                        {
                            range = $"{page.Offset}-{page.Offset + page.Count - 1}/*";
                        }
                        else
                        {
                            range = "*/*";
                        }
                    }

                    //
                    // Content-Range
                    response.Headers.Append(Headers.HeaderNames.ContentRange, $"items {range}");

                    if (page.Count != page.TotalItemsCount)
                    {
                        response.StatusCode = StatusCodes.Status206PartialContent;
                    }
                }
            }
        }

        private void HandleRange(ActionExecutedContext ctx)
        {
            if (!(ctx.Exception is RangeFailedException))
            {
                return;
            }

            ctx.Exception = null;
            ctx.HttpContext.Response.StatusCode = StatusCodes.Status416RangeNotSatisfiable;
        }
    }
}
