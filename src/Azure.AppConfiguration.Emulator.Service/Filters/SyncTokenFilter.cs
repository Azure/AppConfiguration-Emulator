// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.AppConfiguration.Emulator.Service.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.Service
{
    public class SyncTokenFilter : ActionFilterAttribute
    {
        private const string StaticSyncToken = "kv=MA==;sn=1";

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            HttpResponse response = context?.HttpContext?.Response;

            if (response == null)
            {
                return;
            }

            AddResponseHeader(response);
        }

        private static void AddResponseHeader(HttpResponse response)
        {
            if (response.HasStarted)
            {
                return;
            }

            response.OnStarting(() =>
            {
                if (response.StatusCode < StatusCodes.Status500InternalServerError)
                {
                    response.Headers[HeaderNames.SyncToken] = StaticSyncToken;
                }

                return Task.CompletedTask;
            });
        }
    }
}
