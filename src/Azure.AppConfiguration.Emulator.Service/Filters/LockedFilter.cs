// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using Azure.AppConfiguration.Emulator.Integration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Azure.AppConfiguration.Emulator.Service
{
    public class KeyLockedFilter : ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext ctx)
        {
            base.OnActionExecuted(ctx);

            HandleLocked(ctx);
        }

        private void HandleLocked(ActionExecutedContext ctx)
        {
            var e = ctx.Exception as KeyLockedException;

            if (e == null)
            {
                return;
            }

            ctx.Exception = null;
            ctx.HttpContext.Response.StatusCode = StatusCodes.Status409Conflict;

            ctx.Result = new ObjectResult(new
            {
                type = ErrorType.KeyLocked,
                title = $"Modifing key '{e.ParamName}' is not allowed",
                name = e.ParamName,
                detail = $"The key is read-only. To allow modification unlock it first.",
                status = ctx.HttpContext.Response.StatusCode
            });
        }
    }
}
