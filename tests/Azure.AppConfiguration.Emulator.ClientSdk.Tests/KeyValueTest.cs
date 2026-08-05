using Azure;
using Azure.Data.AppConfiguration;
using System.Net;
using Xunit;

namespace Azure.AppConfiguration.Emulator.ClientSdk.Tests
{
    [Collection("ClientSdkCollection")]
    public class KeyValueTest : IAsyncLifetime
    {
        private readonly EmulatorKestrelFixture _fixture;
        private readonly string _resourcePrefix;

        public KeyValueTest(EmulatorKestrelFixture fixture)
        {
            _fixture = fixture;
            _resourcePrefix = $"client-sdk-{Guid.NewGuid().ToString("N")[..12]}-";
        }

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            ConfigurationClient client = _fixture.Client;
            var settings = new List<ConfigurationSetting>();

            await foreach (ConfigurationSetting setting in client.GetConfigurationSettingsAsync(
                new SettingSelector { KeyFilter = $"{_resourcePrefix}*" }))
            {
                settings.Add(setting);
            }

            foreach (ConfigurationSetting setting in settings)
            {
                if (setting.IsReadOnly == true)
                {
                    await client.SetReadOnlyAsync(setting, isReadOnly: false);
                }

                await client.DeleteConfigurationSettingAsync(setting);
            }

            await foreach (ConfigurationSetting setting in client.GetConfigurationSettingsAsync(
                new SettingSelector { KeyFilter = $"{_resourcePrefix}*" }))
            {
                throw new InvalidOperationException($"Failed to clean up test resource '{setting.Key}'.");
            }
        }

        [Fact]
        public async Task SetConfigurationSetting()
        {
            ConfigurationClient client = _fixture.Client;
            string keyPrefix = $"{_resourcePrefix}set-";
            string developmentKey = $"{keyPrefix}development";
            string productionKey = $"{keyPrefix}production";
            string developmentLabel = "development";
            string productionLabel = "production";

            var developmentSetting = new ConfigurationSetting(developmentKey, "development-value", developmentLabel);
            developmentSetting.Tags["environment"] = "development";

            var productionSetting = new ConfigurationSetting(productionKey, "production-value", productionLabel);
            productionSetting.Tags["environment"] = "production";

            await client.SetConfigurationSettingAsync(developmentSetting);
            await client.SetConfigurationSettingAsync(productionSetting);

            var getResponse = await client.GetConfigurationSettingAsync(developmentKey, developmentLabel);
            Assert.Equal("development-value", getResponse.Value.Value);
            Assert.Equal("development", getResponse.Value.Tags["environment"]);

            IReadOnlyList<ConfigurationSetting> byKey = await CollectConfigurationSettings(
                client,
                new SettingSelector { KeyFilter = developmentKey });
            Assert.Single(byKey);
            Assert.Equal(developmentKey, byKey[0].Key);

            IReadOnlyList<ConfigurationSetting> byLabel = await CollectConfigurationSettings(
                client,
                new SettingSelector
                {
                    KeyFilter = $"{keyPrefix}*",
                    LabelFilter = productionLabel
                });
            Assert.Single(byLabel);
            Assert.Equal(productionKey, byLabel[0].Key);

            var tagSelector = new SettingSelector { KeyFilter = $"{keyPrefix}*" };
            tagSelector.TagsFilter.Add("environment=development");

            IReadOnlyList<ConfigurationSetting> byTag = await CollectConfigurationSettings(client, tagSelector);
            Assert.Single(byTag);
            Assert.Equal(developmentKey, byTag[0].Key);
        }

        [Fact]
        public async Task DeleteConfigurationSetting()
        {
            ConfigurationClient client = _fixture.Client;
            string key = $"{_resourcePrefix}delete";
            string label = "delete-test";

            await client.SetConfigurationSettingAsync(key, "value", label);
            var deleteResponse = await client.DeleteConfigurationSettingAsync(key, label);

            Assert.Equal((int)HttpStatusCode.OK, deleteResponse.Status);

            RequestFailedException exception = await Assert.ThrowsAsync<RequestFailedException>(
                async () => await client.GetConfigurationSettingAsync(key, label));

            Assert.Equal((int)HttpStatusCode.NotFound, exception.Status);
        }

        [Fact]
        public async Task SetReadOnly()
        {
            ConfigurationClient client = _fixture.Client;
            string key = $"{_resourcePrefix}lock";

            await client.SetConfigurationSettingAsync(key, "original-value");
            var lockResponse = await client.SetReadOnlyAsync(key, isReadOnly: true);

            Assert.Equal(true, lockResponse.Value.IsReadOnly);

            RequestFailedException updateException = await Assert.ThrowsAsync<RequestFailedException>(
                async () => await client.SetConfigurationSettingAsync(key, "rejected-value"));
            Assert.Equal((int)HttpStatusCode.Conflict, updateException.Status);

            RequestFailedException deleteException = await Assert.ThrowsAsync<RequestFailedException>(
                async () => await client.DeleteConfigurationSettingAsync(key));
            Assert.Equal((int)HttpStatusCode.Conflict, deleteException.Status);

            var unlockResponse = await client.SetReadOnlyAsync(key, isReadOnly: false);
            Assert.Equal(false, unlockResponse.Value.IsReadOnly);

            var updateResponse = await client.SetConfigurationSettingAsync(key, "updated-value");
            Assert.Equal("updated-value", updateResponse.Value.Value);
        }

        [Fact]
        public async Task GetConfigurationSettings()
        {
            const int settingCount = 205;
            ConfigurationClient client = _fixture.Client;
            string keyPrefix = $"{_resourcePrefix}pagination-";

            for (int index = 0; index < settingCount; index++)
            {
                await client.SetConfigurationSettingAsync($"{keyPrefix}{index:D3}", $"value-{index}");
            }

            var pageSizes = new List<int>();
            var returnedKeys = new HashSet<string>();
            var selector = new SettingSelector
            {
                KeyFilter = $"{keyPrefix}*"
            };

            await foreach (Page<ConfigurationSetting> page in client.GetConfigurationSettingsAsync(selector).AsPages())
            {
                pageSizes.Add(page.Values.Count);

                foreach (ConfigurationSetting setting in page.Values)
                {
                    returnedKeys.Add(setting.Key);
                }
            }

            Assert.Equal(new[] { 100, 100, 5 }, pageSizes);
            Assert.Equal(settingCount, returnedKeys.Count);
        }

        private static async Task<IReadOnlyList<ConfigurationSetting>> CollectConfigurationSettings(
            ConfigurationClient client,
            SettingSelector selector)
        {
            var settings = new List<ConfigurationSetting>();

            await foreach (ConfigurationSetting setting in client.GetConfigurationSettingsAsync(selector))
            {
                settings.Add(setting);
            }

            return settings;
        }
    }
}