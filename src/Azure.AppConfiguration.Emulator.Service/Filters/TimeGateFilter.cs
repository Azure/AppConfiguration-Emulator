// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.AppConfiguration.Emulator.Service.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Linq;

namespace Azure.AppConfiguration.Emulator.Service
{
    //
    // Based on RFC7089: https://tools.ietf.org/html/rfc7089
    // HTTP Framework for Time-Based Access to Resource States -- Memento
    // 2.1.1.  Accept-Datetime and Memento-Datetime
    //
    public class TimeGateFilter : ActionFilterAttribute
    {
        private const string BindingArgName = "timeGate";

        public override void OnActionExecuting(ActionExecutingContext ctx)
        {
            if (ctx.ActionDescriptor.Parameters.Any(p => p.Name == BindingArgName))
            {
                ctx.ActionArguments[BindingArgName] = ctx.HttpContext.Request.GetTimeGate();
            }
        }

        public override void OnActionExecuted(ActionExecutedContext ctx)
        {
            base.OnActionExecuted(ctx);

            int status = ctx.HttpContext.Response.StatusCode;

            if (status < 300 || status == StatusCodes.Status404NotFound)
            {
                WriteHeaders(ctx);
            }
        }

        private void WriteHeaders(ActionExecutedContext ctx)
        {
            DateTimeOffset? dt = ctx.HttpContext.Request.GetTimeGate();

            if (dt <= DateTimeOffset.UtcNow)
            {
                HttpResponse response = ctx.HttpContext.Response;

                // Memento-Datetime
                response.Headers[HeaderNames.MementoDatetime] = dt.Value.ToString("r");

                // Link original
                response.Headers.AppendCommaSeparatedValues(HeaderNames.Link, $"<{ctx.HttpContext.Request.GetEncodedPathAndQuery()}>; rel=\"original\"");
            }
        }
    }
}
