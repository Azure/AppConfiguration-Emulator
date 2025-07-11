using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using AspNetTestServer = Microsoft.AspNetCore.TestHost.TestServer;

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
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.ClearProviders();
                })
                .ConfigureServices((IServiceCollection services) =>
                {
                    services.TryAddSingleton<IStoreProvider, StoreProvider>();
                    services.TryAddSingleton<IActiveDirectoryProvider, MockedActiveDirectoryProvider>();
                    services.TryAddSingleton<ICredentialValidator, MockedAdCredentialValidator>();
                    services.TryAddSingleton<AzureAdProvider>();
                    services.TryAddSingleton<IEventPublisher, TestEventPublisher>();

                    services.AddSingleton<IOutputWriter>(Output);
                    services.AddSingleton<AppConfig.Auditing.IOutputWriter>(Output);

                    services.Configure<IdentityOptions>(config.GetSection("Identity"));
                    services.Configure<TenantContext>(config.GetSection("RbacTenant"));

                    services.Configure<HealthOptions>(config.GetSection("Health"));

                    // This is here so that the TestController is recognized by MVC.
                    services.AddMvc()
                    .AddApplicationPart(typeof(TestServer).Assembly);
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