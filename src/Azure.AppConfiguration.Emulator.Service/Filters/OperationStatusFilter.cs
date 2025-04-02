using Azure.AppConfiguration.Emulator.Service.LongRunningOperation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;

namespace Azure.AppConfiguration.Emulator.Service
{
    public class OperationStatusFilter : ActionFilterAttribute
    {
        private static readonly TimeSpan RetryAfter = TimeSpan.FromSeconds(10);

        public override void OnActionExecuted(ActionExecutedContext ctx)
        {
            base.OnActionExecuted(ctx);

            var operationStatus = (ctx.Result as ObjectResult)?.Value as OperationStatus;

            if (operationStatus == null)
            {
                return;
            }

            if (operationStatus.Status == Status.Running)
            {
                ctx.HttpContext.Response.Headers.RetryAfter = ((int)RetryAfter.TotalSeconds).ToString();
            }
        }
    }
}
