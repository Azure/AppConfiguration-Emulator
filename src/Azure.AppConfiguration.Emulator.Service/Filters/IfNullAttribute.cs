using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Azure.AppConfiguration.Emulator.Service
{
    public class IfNullAttribute : ActionFilterAttribute
    {
        int _status;

        public IfNullAttribute(int status)
        {
            _status = status;
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            var result = context.Result as ObjectResult;
            var response = context.HttpContext.Response;

            if (result != null && result.Value == null && response.StatusCode == StatusCodes.Status200OK)
            {
                response.StatusCode = _status;
            }

            base.OnActionExecuted(context);
        }
    }
}
