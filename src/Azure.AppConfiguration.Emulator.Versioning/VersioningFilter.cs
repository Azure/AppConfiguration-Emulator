// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Versioning;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Azure.AppConfiguration.Emulator.Versioning
{
    /// <summary>
    /// Filters out requests based on a configurable list of allowed API versions.
    /// </summary>
    public class VersioningFilter : IActionFilter
    {
        private readonly IErrorResponseProvider _errorResponseProvider;
        private readonly IDictionary<ApiVersion, ApiVersionRule> _allowedApiVersions;

        public VersioningFilter(
            IErrorResponseProvider errorResponseProvider,
            IEnumerable<ApiVersionRule> allowedApiVersions)
        {
            if (errorResponseProvider == null)
            {
                throw new ArgumentNullException(nameof(errorResponseProvider));
            }

            if (allowedApiVersions == null)
            {
                throw new ArgumentNullException(nameof(allowedApiVersions));
            }

            if (!allowedApiVersions.Any())
            {
                throw new ArgumentException("At least one value must be specified.", nameof(allowedApiVersions));
            }

            _errorResponseProvider = errorResponseProvider;
            _allowedApiVersions = allowedApiVersions.ToDictionary(x => ApiVersion.Parse(x.ApiVersion));
        }

        public void OnActionExecuting(ActionExecutingContext ctx)
        {
            ApiVersion requestedApiVersion = ctx.HttpContext.GetRequestedApiVersion();

            if (!_allowedApiVersions.TryGetValue(requestedApiVersion, out var _))
            {
                // Reject the request with UnsupportedApiVersion and don't continue with the pipeline.

                HttpResponse response = ctx.HttpContext.Response;

                response.Clear();

                ErrorResponseContext errorContext = new ErrorResponseContext(
                    ctx.HttpContext.Request,
                    StatusCodes.Status400BadRequest,
                    ErrorCodes.UnsupportedApiVersion,
                    ErrorMessages.UnsupportedApiVersion,
                    messageDetail: null);

                ctx.Result = _errorResponseProvider.CreateResponse(errorContext);
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // nothing to do here.
        }
    }
}
