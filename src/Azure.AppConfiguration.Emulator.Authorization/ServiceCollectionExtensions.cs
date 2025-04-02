// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.AppConfig.Service.Authorization
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAuthorizationPolicy(this IServiceCollection services)
        {
            services.TryAddSingleton<IConfigureOptions<AuthorizationOptions>, ConfigureAuthorizationOptions>();

            return services;
        }
    }
}
