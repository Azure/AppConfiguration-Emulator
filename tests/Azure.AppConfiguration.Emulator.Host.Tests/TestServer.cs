using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using AspNetTestServer = Microsoft.AspNetCore.TestHost.TestServer;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Azure.AppConfiguration.Emulator.Host.Tests
{
    public class TestServer : IDisposable
    {

        private readonly IWebHostBuilder _webHostBuilder;
        private AspNetTestServer _server;
        private HttpClient _httpClient;

        public TestServer()
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddJsonFile("appsettings.json")
                .Build();

            _webHostBuilder = WebHost.CreateDefaultBuilder()
                .UseConfiguration(config)
                .ConfigureServices((IServiceCollection services) =>
                {
                    services.TryAddSingleton<IKeyValueStorage, TestKeyValueStorage>();

                })
                .UseStartup<Startup>();
        }

        public AspNetTestServer Server
        {
            get
            {
                if (_server == null)
                {
                    _server = new AspNetTestServer(_webHostBuilder);
                }

                return _server;
            }
        }

        public HttpClient ServerClient
        {
            get
            {
                if (_httpClient == null)
                {
                    HttpClient http = Server.CreateClient();

                    http.Timeout = TimeSpan.FromMinutes(1);

                    _httpClient = http;
                }

                return _httpClient;
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
            _server?.Dispose();
        }
    }
}