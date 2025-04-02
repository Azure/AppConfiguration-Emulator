// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Azure.AppConfiguration.Emulator.Service.Validators
{
    public class AllowVersionedParameterAttribute : ActionFilterAttribute
    {
        private readonly string _parameterName;
        private readonly ApiVersion _minApiVersion;

        public AllowVersionedParameterAttribute(string name, string minApiVersion)
        {
            _parameterName = name ?? throw new ArgumentNullException(nameof(name));
            _minApiVersion = ApiVersion.Parse(minApiVersion ?? throw new ArgumentNullException(nameof(minApiVersion)));
        }

        public override void OnActionExecuting(ActionExecutingContext ctx)
        {
            if (ctx == null)
            {
                throw new ArgumentNullException(nameof(ctx));
            }

            ApiVersion apiVersion = ctx.HttpContext?.GetRequestedApiVersion();

            if (apiVersion == null || apiVersion >= _minApiVersion)
            {
                return;
            }

            IEnumerable<ParameterDescriptor> parameters = ctx.ActionDescriptor?.Parameters;

            if (parameters != null && parameters.Any(p => p.Name == _parameterName))
            {
                ctx.ActionArguments[_parameterName] = null;
            }

            base.OnActionExecuting(ctx);
        }
    }
}
