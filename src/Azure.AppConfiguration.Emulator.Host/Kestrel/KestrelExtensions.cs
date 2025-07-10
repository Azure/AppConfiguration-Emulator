// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using System;
using System.Configuration;
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

            if (string.IsNullOrEmpty(hostingConfiguration.IPAddress))
            {
                throw new ArgumentNullException(nameof(hostingConfiguration.IPAddress));
            }

            if (!IPAddress.TryParse(hostingConfiguration.IPAddress, out IPAddress ipAddress))
            {
                throw new ConfigurationErrorsException($"Invalid IP address '{hostingConfiguration.IPAddress}' in configuration.");
            }

            //
            // Listen on configured ports
            options.Listen(ipAddress, hostingConfiguration.Port, listenOptions =>
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
