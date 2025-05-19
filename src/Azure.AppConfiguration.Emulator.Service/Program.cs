// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.AppConfiguration.Emulator.Hosting;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Azure.AppConfiguration.Emulator.Service
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args)
        {
            return WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddEnvironmentVariables();
                    config.AddCommandLine(args);
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                })
                .UseStartup<Startup>()
                .UseKestrel(o =>
                {
                    //
                    // Request limits
                    //
                    o.Limits.MaxRequestLineSize = 1 << 10; // 1KB
                    o.Limits.MaxRequestHeadersTotalSize = 32 << 10; // 32KB
                    o.Limits.MaxRequestBodySize = 10 << 10; // 10KB

                    //
                    // Read HostingConfiguration from configuration
                    HostingConfiguration hostingConfiguration = o.ApplicationServices.GetRequiredService<IOptions<HostingConfiguration>>().Value;

                    //
                    // Configure host ports and SSL protocols
                    o.Configure(hostingConfiguration);
                })
                .Build();
        }
    }
}
