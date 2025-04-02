using Azure.AppConfiguration.Emulator.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AppConfig.Service.Authentication
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddHmacAuthentication(this IServiceCollection services)
        {
            //
            // Validator
            services.AddSingleton<ICredentialValidator, HmacSha256CredentialValidator>();

            return services;
        }
    }
}
