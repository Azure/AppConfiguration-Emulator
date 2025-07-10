// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using System;
using System.Net;

namespace Azure.AppConfiguration.Emulator.Host
{
    static class KestrelExtensions
    {
        public static KestrelServerOptions Configure(this KestrelServerOptions options, HostingConfiguration hostingConfiguration)
        {
            if (hostingConfiguration == null)
            {
                throw new ArgumentNullException(nameof(hostingConfiguration));
            }

            if (hostingConfiguration.Port <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(hostingConfiguration.Port));
            }

            bool useHttps = !string.IsNullOrEmpty(hostingConfiguration.PFX);

            if (useHttps && hostingConfiguration.Certificate == null)
            {
                throw new ArgumentNullException(nameof(hostingConfiguration.Certificate));
            }

            //
            // Listen on configured ports
            options.Listen(IPAddress.Loopback, hostingConfiguration.Port, listenOptions =>
            {
                if (useHttps)
                {
                    listenOptions.UseHttps(
                        new HttpsConnectionAdapterOptions
                        {
                            ClientCertificateMode = ClientCertificateMode.NoCertificate,
                            ServerCertificate = hostingConfiguration.Certificate
                        });
                }
            });

            return options;
        }
    }
}
