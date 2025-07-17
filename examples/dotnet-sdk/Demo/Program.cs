using Azure.Core;
using Azure.Data.AppConfiguration;
using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;
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
                throw new InvalidOperationException("APP_CONFIGURATION_EMULATOR_CONNECTION_STRING environment variable not found. Please run the start-emulator.ps1 script first.");
            }

            var client = new ConfigurationClient(connectionString);
            await client.SetConfigurationSettingAsync(new ConfigurationSetting("testkey", "testvalue"));

            // Create a feature flag
            await client.SetConfigurationSettingAsync(
                ".appconfig.featureflag/test-feature-flag",
                "{\"id\":\"test-feature-flag\",\"enabled\":true}",
                ContentType: "application/vnd.microsoft.appconfig.ff+json;charset=utf-8");

            // For information about Azure App Configuration provider, please go to: https://learn.microsoft.com/azure/azure-app-configuration/configuration-provider-overview
            var builder = new ConfigurationBuilder();
            builder.AddAzureAppConfiguration(options =>
            {
                options.Connect(connectionString);
            });

            IConfiguration configuration = builder.Build();

            string testValue = configuration["testkey"];
            Console.WriteLine($"Configuration setting for 'testkey': {testValue}");

            var featureManager = new FeatureManager(
                new ConfigurationFeatureDefinitionProvider(configuration));

            Console.WriteLine($"'test-feature-flag': {await featureManager.IsEnabledAsync("test-feature-flag")}");
        }
    }
}
