using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using System;

namespace Azure.AppConfiguration.Emulator.Service
{
    public class NotAllowedFilter : ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext ctx)
        {
            base.OnActionExecuted(ctx);

            HandleNotImplemented(ctx);
        }

        private void HandleNotImplemented(ActionExecutedContext ctx)
        {
            var e = ctx.Exception as NotImplementedException;

            if (e == null)
            {
                return;
            }

            ctx.Exception = null;
            ctx.HttpContext.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
        }
    }
}
