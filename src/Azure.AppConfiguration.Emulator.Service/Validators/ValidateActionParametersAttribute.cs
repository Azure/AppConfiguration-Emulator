using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace Azure.AppConfiguration.Emulator.Service.Validators
{
    public class ValidateActionParametersAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext ctx)
        {
            var descriptor = ctx.ActionDescriptor as ControllerActionDescriptor;

            if (descriptor != null)
            {
                foreach (var param in descriptor.MethodInfo.GetParameters())
                {
                    ctx.ActionArguments.TryGetValue(param.Name, out object arg);

                    if (!ctx.ModelState.IsValid || !IsValid(param, arg))
                    {
                        ctx.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

                        string invalidEntry = ctx.ModelState.FirstOrDefault(kv => kv.Value.ValidationState == ModelValidationState.Invalid).Key;

                        if (!string.IsNullOrEmpty(invalidEntry))
                        {
                            ctx.Result = new ObjectResult(invalidEntry);
                        }
                        else if (!string.IsNullOrEmpty(param.Name))
                        {
                            ctx.Result = new ObjectResult(param.Name);
                        }
                        else
                        {
                            ctx.Result = new EmptyResult();
                        }

                        break;
                    }
                }
            }

            base.OnActionExecuting(ctx);
        }

        private bool IsValid(ParameterInfo parameter, object argument)
        {
            foreach (var attrData in parameter.CustomAttributes)
            {
                var attribute = parameter.GetCustomAttribute(attrData.AttributeType) as ValidationAttribute;

                if (attribute != null && !attribute.IsValid(argument))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
