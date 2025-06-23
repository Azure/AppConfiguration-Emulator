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
        string distPath = Path.GetFullPath(Path.Combine(hostPath, "ClientApp/dist"));

        if (!Directory.Exists(distPath))
        {
            throw new DirectoryNotFoundException($"'{distPath}' does not exist. Ensure the ClientApp has been built.");
        }

        services.AddSpaStaticFiles(
            x =>
            {
                x.RootPath = distPath;
            });

        return services;
    }
}
