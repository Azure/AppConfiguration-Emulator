// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.AppConfiguration.Emulator.Authentication.EntraId;
using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using Azure.AppConfiguration.Emulator.ConfigurationSnapshots;
using Azure.AppConfiguration.Emulator.Integration;
using Azure.AppConfiguration.Emulator.Service;
using Azure.AppConfiguration.Emulator.Service.Filters;
using Azure.AppConfiguration.Emulator.Service.Formatters.Json;
using Azure.AppConfiguration.Emulator.Service.Validators;
using Azure.AppConfiguration.Emulator.Tenant;
using Azure.AppConfiguration.Emulator.Versioning;
using Microsoft.AppConfig.Service.Authentication.Anonymous;
using Microsoft.AppConfig.Service.Authentication.Hmac;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Azure.AppConfiguration.Emulator.Host
{
    public class Startup
    {
        private readonly IHostEnvironment _hostingEnvironment;

        public Startup(IConfiguration configuration, IHostEnvironment env)
        {
            _hostingEnvironment = env;
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.TryAddSingleton(sp => services);

            //
            // Options
            ConfigureOptions(services);

            //
            // Logging
            services.AddHttpLogging(o => { });

            //
            // Authentication
            services.AddApiAuthentication();

            //
            // Authorization
            services.AddApiAuthorization();

            //
            // Http Client
            services.AddHttpClientFactory();

            //
            // MVC
            services.AddMvc(o =>
            {
                o.ValueProviderFactories.Insert(0, new DecodeSlashRouteValueProviderFactory());

                o.Filters.Add(new SyncTokenFilter());
                o.Filters.Add(new PreConditionFilter());
                o.Filters.Add(new NotFoundFilter());
                o.Filters.Add(new BadRequestFilter());
                o.Filters.Add(new NotAllowedFilter());
            })
            .ConfigureApiBehaviorOptions(o =>
            {
                //
                // Enable model validation through custom validation filter
                o.SuppressModelStateInvalidFilter = true;
            })
            .AddKeyValueJsonFormatter();

            //
            // API Versioning
            services.AddVersioning();

            //
            // Configuration setting
            services.AddConfigurationSettings();

            //
            // Snapshots
            services.AddConfigurationSnapshots();

            //
            // UI
            services.AddUI();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostEnvironment env)
        {
            app.UseDiagnostics();

            app.UseSpaStaticFiles();

            app.UseSystemErrors()
               .UseRouting()
               .UseAuthentication()
               .UseAuthorization()
               .UsePathValidation()
               .UseEndpoints(endpoints =>
               {
                   endpoints.MapControllers();
               });

            app.UseUI();
        }

        private void ConfigureOptions(IServiceCollection services)
        {
            //
            // Hosting
            services.Configure<HostingConfiguration>(Configuration.GetSection("Hosting"));

            //
            // Tenant
            services.Configure<TenantOptions>(Configuration.GetSection("Tenant"));

            //
            // KeyValueStorage
            services.Configure<KeyValueStorageOptions>(Configuration.GetSection("KeyValueStorage"));

            //
            // Snapshot Provider
            services.Configure<SnapshotProviderOptions>(Configuration.GetSection("SnapshotProvider"));

            //
            // Hmac
            services.Configure<HmacOptions>(Configuration.GetSection("Authentication:Hmac"));

            //
            // EntraId
            services.Configure<EntraIdOptions>(Configuration.GetSection("Authentication:EntraId"));

            //
            // Anonymous authentication
            services.Configure<AnonymousOptions>(Configuration.GetSection("Authentication:Anonymous"));

            //
            // Versioning
            services.Configure<VersioningOptions>(Configuration.GetSection("Versioning"));

            //
            // Http
            services.Configure<HttpOptions>(Configuration.GetSection("Http"));
        }
    }
}
