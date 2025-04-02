using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Azure.AppConfiguration.Emulator.Service
{
    static class VersioningExtensions
    {
        public static IServiceCollection AddVersioning(this IServiceCollection services)
        {
            services.AddTransient<IConfigureOptions<ApiVersioningOptions>, ConfigureApiVersioningOptions>();

            return services.AddApiVersioning();
        }
    }
}
