using AspNetTestServer = Microsoft.AspNetCore.TestHost.TestServer;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Azure.AppConfiguration.Emulator.Host.Tests
{
    public class TestServer : ITestServer, IDisposable
    {

        private readonly IWebHostBuilder _webHostBuilder;
        private AspNetTestServer _server;
        private HttpClient _httpClient;

        public TestServer()
        {
            // Delete the storage file to ensure a clean state for tests
            DeleteStorageFile();

            IConfiguration config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddJsonFile("appsettings.json")
                .Build();

            _webHostBuilder = WebHost.CreateDefaultBuilder()
                .UseConfiguration(config)
                .UseStartup<Startup>();
        }

        private void DeleteStorageFile()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

            string storageDirectory = Path.Combine(baseDirectory, ".aace");
            string kvFilePath = Path.Combine(storageDirectory, "kv.ndjson");
            string snapshotFilePath = Path.Combine(storageDirectory, "snapshot.ndjson");
            string snapshotFolderPath = Path.Combine(storageDirectory, "snapshots");

            if (File.Exists(kvFilePath))
            {
                File.Delete(kvFilePath);
                Console.WriteLine($"Deleted storage file: {kvFilePath}");
            }

            if (File.Exists(snapshotFilePath))
            {
                File.Delete(snapshotFilePath);
                Console.WriteLine($"Deleted storage file: {snapshotFilePath}");
            }

            if (Directory.Exists(snapshotFolderPath))
            {
                Directory.Delete(snapshotFolderPath, true);
                Console.WriteLine($"Deleted storage directory: {snapshotFolderPath}");
            }
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

        public HttpClient Client
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
            DeleteStorageFile();

            _httpClient?.Dispose();
            _server?.Dispose();
        }
    }
}
