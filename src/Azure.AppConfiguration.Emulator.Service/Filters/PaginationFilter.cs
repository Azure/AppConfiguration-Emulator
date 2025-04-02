using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using Azure.AppConfiguration.Emulator.Service.Http;
using Azure.AppConfiguration.Emulator.Service.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.WebUtilities;
using System.Linq;

namespace Azure.AppConfiguration.Emulator.Service
{
    public class PaginationFilter : ActionFilterAttribute
    {
        private const string BindingArgName = "after";

        public override void OnActionExecuting(ActionExecutingContext ctx)
        {
            if (ctx.ActionDescriptor.Parameters.Any(p => p.Name == BindingArgName))
            {
                ctx.ActionArguments[BindingArgName] = ctx.HttpContext.Request.GetAfter();
            }
        }

        public override void OnActionExecuted(ActionExecutedContext ctx)
        {
            base.OnActionExecuted(ctx);

            WriteHeaders(ctx);
        }

        private void WriteHeaders(ActionExecutedContext ctx)
        {
            IPage page = (ctx.Result as ObjectResult)?.Value as IPage;

            if (page == null)
            {
                return;
            }

            //
            // Link next
            if (page.ContinuationToken != null)
            {
                HttpResponse response = ctx.HttpContext.Response;
                HttpRequest request = ctx.HttpContext.Request;

                page.NextLink = ReplaceQueryParam(request, "after", page.ContinuationToken.Base64Encode());

                response.Headers.Append(HeaderNames.Link, $"<{page.NextLink}>; rel=\"next\"");
            }
        }

        private static string ReplaceQueryParam(HttpRequest request, string key, string value)
        {
            string url = RemoveQueryParam(request, key);
            return QueryHelpers.AddQueryString(url, key, value);
        }

        private static string RemoveQueryParam(HttpRequest request, string key)
        {
            var qb = new QueryBuilder();

            foreach (var q in request.Query)
            {
                if (!q.Key.EqualsIgnoreCase(key))
                {
                    qb.Add(q.Key, (string)q.Value);
                }
            }

            return request.Path + qb.ToQueryString();
        }
    }
}
