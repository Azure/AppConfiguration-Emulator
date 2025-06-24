// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

using System.Reflection;

public static class IntegrationExtentions
{
    public static IApplicationBuilder UseUI(this IApplicationBuilder app)
    {
        app.UseSpa(
            x =>
            {
                x.Options.SourcePath = "ClientApp";
            });

        return app;
    }

    public static IServiceCollection AddUI(this IServiceCollection services)
    {
        string hostPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        // Try wwwroot first (where publish puts files)
        string wwwrootPath = Path.GetFullPath(Path.Combine(hostPath, "wwwroot"));

        // Fall back to ClientApp/dist (for development)
        string distPath = Path.GetFullPath(Path.Combine(hostPath, "ClientApp/dist"));

        string rootPath;

        if (Directory.Exists(wwwrootPath))
        {
            rootPath = wwwrootPath;
        }
        else if (Directory.Exists(distPath))
        {
            rootPath = distPath;
        }
        else
        {
            throw new DirectoryNotFoundException($"Neither '{wwwrootPath}' nor '{distPath}' exists. Ensure the ClientApp has been built.");
        }

        services.AddSpaStaticFiles(
            x =>
            {
                x.RootPath = rootPath;
            });

        return services;
    }
}
