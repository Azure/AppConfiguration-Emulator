// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.AppConfiguration.Emulator.Authentication;
using Azure.AppConfiguration.Emulator.Authentication.EntraId;
using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using Azure.AppConfiguration.Emulator.ConfigurationSnapshots;
using Azure.AppConfiguration.Emulator.Diagnostics;
using Microsoft.AppConfig.Service.Authentication;
using Microsoft.AppConfig.Service.Authentication.Anonymous;
using Microsoft.AppConfig.Service.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System.Net.Http;

namespace Azure.AppConfiguration.Emulator.Integration
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApiAuthentication(this IServiceCollection services)
        {
            //
            // IHttpContextAccessor
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            //
            // Credentials
            services.TryAddSingleton<ICredentialResolver, AuthorizeCredentialResolver>();
            services.AddScoped(sp => sp.GetRequiredService<ICredentialResolver>().GetCredential());

            //
            // Configure authentication
            services.TryAddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureBearerOptions>();

            //
            // Authentication
            AuthenticationBuilder builder = services.AddAuthentication();

            //
            // Hmac authentication
            services.AddHmacAuthentication();
            builder.AddJwtBearer(AuthenticationShemes.HmacSha256, o => { });

            //
            // EntraId authentication
            services.AddEntraIdAuthentication();
            builder.AddJwtBearer(AuthenticationShemes.EntraId, o => { });

            //
            // Anonymous authentication
            services.AddAnonymousAuthentication();
            builder.AddJwtBearer(AuthenticationShemes.Anonymous, o => { });

            return services;
        }

        public static IServiceCollection AddApiAuthorization(this IServiceCollection services)
        {
            return services
                .AddAuthorizationPolicy()
                .AddAuthorization();
        }

        public static IServiceCollection AddConfigurationSnapshots(this IServiceCollection services)
        {
            services.TryAddSingleton<ISnapshotsStorage, SnapshotsStorage>();
            services.TryAddSingleton<ISnapshotProvider, SnapshotProvider>();

            return services;
        }

        public static IServiceCollection AddConfigurationSettings(this IServiceCollection services)
        {
            services.TryAddSingleton<IKeyValueStorage, KeyValueStorage>();

            services.TryAddSingleton<KeyValueProvider>();
            services.TryAddSingleton<IKeyValueProvider>(sp => sp.GetRequiredService<KeyValueProvider>());
            services.TryAddSingleton<IKeyProvider>(sp => sp.GetRequiredService<KeyValueProvider>());
            services.TryAddSingleton<ILabelProvider>(sp => sp.GetRequiredService<KeyValueProvider>());
            services.TryAddSingleton<IRevisionProvider>(sp => sp.GetRequiredService<KeyValueProvider>());

            services.AddHostedService(sp => sp.GetRequiredService<KeyValueProvider>());

            return services;
        }

        public static IApplicationBuilder UseSystemErrors(this IApplicationBuilder services)
        {
            //
            // Middlewares instead of MVC filters because we can get here before MVC pipeline starts
            return services
                .UseMiddleware<ServiceUnavailableMiddleware>()
                .UseMiddleware<RequestAbortedMiddleware>()
                .UseMiddleware<ConflictMiddleware>();
        }

        public static IApplicationBuilder UseDiagnostics(this IApplicationBuilder services)
        {
            return services
                .UseMiddleware<DiagnosticsMiddleware>()
                .UseHttpLogging();
        }

        public static IHttpClientBuilder AddHttpClientFactory(this IServiceCollection services)
        {
            IHttpClientBuilder builder = services.AddHttpClient(Options.DefaultName)
                    .ConfigurePrimaryHttpMessageHandler((serviceProvider) =>
                    {
                        HttpOptions httpOptions = serviceProvider.GetRequiredService<IOptions<HttpOptions>>().Value;
                        HttpClientHandler clientHandler = new HttpClientHandler();
                        if (!httpOptions.DisableCertificateRevocationListChecking)
                        {
                            clientHandler.CheckCertificateRevocationList = true;
                        }

                        return clientHandler;
                    });

            services.TryAddTransient(s =>
            {
                return s.GetRequiredService<IHttpClientFactory>().CreateClient(Options.DefaultName);
            });

            return builder;
        }
    }
}
