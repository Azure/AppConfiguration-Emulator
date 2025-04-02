// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Azure.AppConfiguration.Emulator.Service
{
    public class NotFoundFilter : ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext ctx)
        {
            base.OnActionExecuted(ctx);

            if (ctx.HttpContext.Response.StatusCode == StatusCodes.Status404NotFound)
            {
                ctx.Result = new EmptyResult();
            }
        }
    }
}
