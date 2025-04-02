using Azure.AppConfiguration.Emulator.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AppConfig.Service.Authentication.Anonymous
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAnonymousAuthentication(this IServiceCollection services)
        {
            //
            // Validator
            services.AddSingleton<ICredentialValidator, AnonymousCredentialValidator>();

            return services;
        }
    }
}
