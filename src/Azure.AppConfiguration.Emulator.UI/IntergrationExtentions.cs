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
        app.UseStaticFiles();
        app.UseSpaStaticFiles();
        app.UseSpa(
            x =>
            {
                x.Options.SourcePath = "wwwroot";
            });

        return app;
    }

    public static IServiceCollection AddUI(this IServiceCollection services)
    {
        string hostPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        string wwwrootPath = Path.GetFullPath(Path.Combine(hostPath, "wwwroot"));

        if (!Directory.Exists(wwwrootPath))
        {
            throw new DirectoryNotFoundException($"'{wwwrootPath}' does not exist. Ensure the UI has been built.");
        }

        services.AddSpaStaticFiles(
            x =>
            {
                x.RootPath = wwwrootPath;
            });

        return services;
    }
}
