// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.AppConfig.Service.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.Authentication
{
    public class ConfigureBearerOptions : IConfigureNamedOptions<JwtBearerOptions>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IEnumerable<ICredentialValidator> _validators;

        public ConfigureBearerOptions(
            IHttpContextAccessor httpContextAccessor,
            IEnumerable<ICredentialValidator> validators)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _validators = validators ?? throw new ArgumentNullException(nameof(validators));
        }

        public void Configure(string scheme, JwtBearerOptions options)
        {
            ICredentialValidator validator = _validators.FirstOrDefault(v => v.CanValidate(scheme));

            // 
            // Scheme - use "Anonymous" for empty scheme to avoid WWW-Authenticate header with empty value
            options.Challenge = string.IsNullOrEmpty(scheme) ? "Anonymous" : scheme;

            //
            // Handler
            options.Events = options.Events ?? new JwtBearerEvents();
            options.Events.OnMessageReceived = async ctx =>
            {
                HttpContext context = _httpContextAccessor.HttpContext;

                if (context == null)
                {
                    ctx.NoResult();
                    return;
                }

                Credential credential = context.RequestServices.GetService<Credential>();

                if (credential == null ||
                    validator == null ||
                    !validator.CanValidate(credential.Scheme))
                {
                    ctx.NoResult();
                    return;
                }

                //
                // Ensure validators can inspect request body
                context.Request.EnableBuffering();

                CredentialValidationResult result = await validator.Validate(credential, context.RequestAborted);

                if (result.HasSucceeded)
                {
                    ctx.Principal = result.Principal;

                    ctx.Success();
                }
                else
                {
                    ctx.Fail(result.Error);
                }
            };

            options.Events.OnChallenge = ctx =>
            {
                if (validator.CanChallenge())
                {
                    if (ctx.AuthenticateFailure != null)
                    {
                        ctx.ErrorDescription = ctx.AuthenticateFailure.Message;

                        if (string.Equals(ctx.ErrorDescription, Errors.SchemeNotAllowed))
                        {
                            ctx.Error = "authentication_disabled";
                        }
                    }
                }
                else
                {
                    ctx.HandleResponse();
                }

                return Task.CompletedTask;
            };
        }

        public void Configure(JwtBearerOptions options)
        {
            Configure(string.Empty, options);
        }
    }
}
