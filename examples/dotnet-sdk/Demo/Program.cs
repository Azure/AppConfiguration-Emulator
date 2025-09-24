using Azure.Data.AppConfiguration;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace AppConfigurationEmulatorDemo
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string connectionString = Environment.GetEnvironmentVariable("APP_CONFIGURATION_EMULATOR_CONNECTION_STRING");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("APP_CONFIGURATION_EMULATOR_CONNECTION_STRING environment variable not found. Please run the start-emulator script first.");
            }

            var client = new ConfigurationClient(connectionString);
            await client.SetConfigurationSettingAsync(new ConfigurationSetting("testkey", "testvalue"));

            // For information about Azure App Configuration provider, please go to: https://learn.microsoft.com/azure/azure-app-configuration/configuration-provider-overview
            var builder = new ConfigurationBuilder();
            builder.AddAzureAppConfiguration(options =>
            {
                options.Connect(connectionString);
            });

            IConfiguration configuration = builder.Build();

            string testValue = configuration["testkey"];
            Console.WriteLine($"Configuration setting for 'testkey': {testValue}");
        }
    }
}
