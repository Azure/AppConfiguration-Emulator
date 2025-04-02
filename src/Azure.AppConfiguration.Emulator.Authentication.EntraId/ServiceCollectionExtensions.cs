using Microsoft.AppConfig.Service.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Azure.AppConfiguration.Emulator.Authentication.EntraId
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddEntraIdAuthentication(this IServiceCollection services)
        {
            //
            // Authorization
            services.TryAddSingleton<IAuthorizationProvider, AuthorizationProvider>();

            //
            // Validator
            services.AddSingleton<ICredentialValidator, EntraIdCredentialValidator>();

            return services;
        }
    }
}
