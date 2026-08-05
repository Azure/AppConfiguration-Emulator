using Azure.AppConfiguration.Emulator.Host;
using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Data.AppConfiguration;
using Microsoft.AspNetCore.Hosting;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace Azure.AppConfiguration.Emulator.ClientSdk.Tests
{
    public sealed class EmulatorKestrelFixture : IAsyncLifetime
    {
        private IWebHost _host;
        private HttpClient _httpClient;
        private X509Certificate2 _serverCertificate;

        public ConfigurationClient Client { get; private set; }

        public Uri Endpoint { get; private set; }

        public async Task InitializeAsync()
        {
            int port = GetAvailablePort();
            _serverCertificate = CreateServerCertificate();

            string serverCertificate = Convert.ToBase64String(_serverCertificate.Export(X509ContentType.Pfx));

            string[] args =
            {
                "--Hosting:IPAddress=127.0.0.1",
                $"--Hosting:Port={port}",
                $"--Hosting:PFX={serverCertificate}",
                "--Tenant:AnonymousAuthEnabled=true",
                "--Tenant:OutputPageSize=100",
                "--SnapshotProvider:OutputPageSize=100",
                "--Authentication:Anonymous:AnonymousUserRole=Owner",
                "--Logging:LogLevel:Default=Warning",
            };

            _host = Program.BuildWebHost(args);
            await _host.StartAsync();

            Endpoint = new Uri($"https://127.0.0.1:{port}");

            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (_, certificate, _, _) =>
                string.Equals(certificate?.Thumbprint, _serverCertificate.Thumbprint, StringComparison.OrdinalIgnoreCase);

            _httpClient = new HttpClient(handler);
            var options = new ConfigurationClientOptions
            {
                Transport = new HttpClientTransport(_httpClient)
            };
            options.AddPolicy(new AnonymousAuthenticationPolicy(), HttpPipelinePosition.PerRetry);

            Client = new ConfigurationClient(Endpoint, new AnonymousTokenCredential(), options);
        }

        public async Task DisposeAsync()
        {
            _httpClient?.Dispose();

            if (_host != null)
            {
                await _host.StopAsync();
                _host.Dispose();
            }

            _serverCertificate?.Dispose();
        }

        private static int GetAvailablePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();

            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();

            return port;
        }

        private static X509Certificate2 CreateServerCertificate()
        {
            using RSA rsa = RSA.Create(2048);
            var request = new CertificateRequest("CN=localhost", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            var subjectAlternativeName = new SubjectAlternativeNameBuilder();
            subjectAlternativeName.AddDnsName("localhost");
            subjectAlternativeName.AddIpAddress(IPAddress.Loopback);

            request.CertificateExtensions.Add(subjectAlternativeName.Build());
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, false));

            return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(1));
        }
    }

    [CollectionDefinition("ClientSdkCollection")]
    public sealed class ClientSdkCollection : ICollectionFixture<EmulatorKestrelFixture>
    {
    }
}