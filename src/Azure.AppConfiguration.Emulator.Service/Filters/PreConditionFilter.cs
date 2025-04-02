using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Azure.AppConfiguration.Emulator.Service
{
    /// <summary>
    /// Handles HTTP request's precondition 
    /// (rfc: If-Match, If-None-Match)
    /// </summary>
    public class PreConditionFilter : ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext ctx)
        {
            base.OnActionExecuted(ctx);

            if (ctx.Exception is MatchFailedException)
            {
                ctx.Exception = null;
                ctx.HttpContext.Response.StatusCode = StatusCodes.Status412PreconditionFailed;
            }
        }
    }
}
