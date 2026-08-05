using Azure;
using Azure.Data.AppConfiguration;
using Xunit;

namespace Azure.AppConfiguration.Emulator.ClientSdk.Tests
{
    [Collection("ClientSdkCollection")]
    public class SnapshotTest : IAsyncLifetime
    {
        private readonly EmulatorKestrelFixture _fixture;
        private readonly string _keyPrefix;
        private readonly string _snapshotPrefix;

        public SnapshotTest(EmulatorKestrelFixture fixture)
        {
            _fixture = fixture;

            string testHash = Guid.NewGuid().ToString("N")[..12];
            _keyPrefix = $"client-sdk-{testHash}-snapshot-kv-";
            _snapshotPrefix = $"client-sdk-{testHash}-snapshot-";
        }

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            ConfigurationClient client = _fixture.Client;
            var snapshotSelector = new SnapshotSelector
            {
                NameFilter = $"{_snapshotPrefix}*"
            };
            snapshotSelector.Status.Add(ConfigurationSnapshotStatus.Ready);
            snapshotSelector.Status.Add(ConfigurationSnapshotStatus.Archived);

            await foreach (ConfigurationSnapshot snapshot in client.GetSnapshotsAsync(snapshotSelector))
            {
                if (snapshot.Status == ConfigurationSnapshotStatus.Ready)
                {
                    await client.ArchiveSnapshotAsync(snapshot.Name);
                }
            }

            var settings = new List<ConfigurationSetting>();
            await foreach (ConfigurationSetting setting in client.GetConfigurationSettingsAsync(
                new SettingSelector { KeyFilter = $"{_keyPrefix}*" }))
            {
                settings.Add(setting);
            }

            foreach (ConfigurationSetting setting in settings)
            {
                await client.DeleteConfigurationSettingAsync(setting);
            }

            var readySelector = new SnapshotSelector
            {
                NameFilter = $"{_snapshotPrefix}*"
            };
            readySelector.Status.Add(ConfigurationSnapshotStatus.Ready);

            await foreach (ConfigurationSnapshot snapshot in client.GetSnapshotsAsync(readySelector))
            {
                throw new InvalidOperationException($"Failed to archive test snapshot '{snapshot.Name}'.");
            }

            await foreach (ConfigurationSetting setting in client.GetConfigurationSettingsAsync(
                new SettingSelector { KeyFilter = $"{_keyPrefix}*" }))
            {
                throw new InvalidOperationException($"Failed to clean up snapshot source setting '{setting.Key}'.");
            }
        }

        [Fact]
        public async Task CreateSnapshot()
        {
            ConfigurationClient client = _fixture.Client;
            string developmentKey = $"{_keyPrefix}development";
            string productionKey = $"{_keyPrefix}production";
            string snapshotName = $"{_snapshotPrefix}create";

            await client.SetConfigurationSettingAsync(developmentKey, "development-value", "development");
            await client.SetConfigurationSettingAsync(productionKey, "production-value", "production");

            var developmentFilter = new ConfigurationSettingsFilter(developmentKey)
            {
                Label = "development"
            };
            var productionFilter = new ConfigurationSettingsFilter(productionKey)
            {
                Label = "production"
            };
            var snapshot = new ConfigurationSnapshot(new[] { developmentFilter, productionFilter })
            {
                SnapshotComposition = SnapshotComposition.Key
            };
            snapshot.Tags["source"] = "client-sdk-test";

            CreateSnapshotOperation operation = await client.CreateSnapshotAsync(
                WaitUntil.Completed,
                snapshotName,
                snapshot);

            Assert.True(operation.HasCompleted);
            Assert.Equal(ConfigurationSnapshotStatus.Ready, operation.Value.Status);
            Assert.Equal(2, operation.Value.ItemCount);

            var snapshotSettings = new List<ConfigurationSetting>();
            await foreach (ConfigurationSetting setting in client.GetConfigurationSettingsForSnapshotAsync(snapshotName))
            {
                snapshotSettings.Add(setting);
            }

            Assert.Equal(2, snapshotSettings.Count);
            Assert.Contains(snapshotSettings, setting => setting.Key == developmentKey && setting.Value == "development-value");
            Assert.Contains(snapshotSettings, setting => setting.Key == productionKey && setting.Value == "production-value");
        }

        [Fact]
        public async Task GetSnapshot()
        {
            ConfigurationClient client = _fixture.Client;
            string snapshotName = $"{_snapshotPrefix}get";

            await CreateReadySnapshot(snapshotName, "get");
            var response = await client.GetSnapshotAsync(snapshotName);

            Assert.Equal(snapshotName, response.Value.Name);
            Assert.Equal(ConfigurationSnapshotStatus.Ready, response.Value.Status);
            Assert.Equal(1, response.Value.ItemCount);
            Assert.Equal("client-sdk-test", response.Value.Tags["source"]);
        }

        [Fact]
        public async Task GetSnapshots()
        {
            ConfigurationClient client = _fixture.Client;
            string firstSnapshotName = $"{_snapshotPrefix}list-1";
            string secondSnapshotName = $"{_snapshotPrefix}list-2";

            await CreateReadySnapshot(firstSnapshotName, "list-1");
            await CreateReadySnapshot(secondSnapshotName, "list-2");

            var selector = new SnapshotSelector
            {
                NameFilter = $"{_snapshotPrefix}list-*"
            };
            selector.Status.Add(ConfigurationSnapshotStatus.Ready);

            var snapshots = new List<ConfigurationSnapshot>();
            await foreach (ConfigurationSnapshot snapshot in client.GetSnapshotsAsync(selector))
            {
                snapshots.Add(snapshot);
            }

            Assert.Equal(2, snapshots.Count);
            Assert.Contains(snapshots, snapshot => snapshot.Name == firstSnapshotName);
            Assert.Contains(snapshots, snapshot => snapshot.Name == secondSnapshotName);
        }

        [Fact]
        public async Task ArchiveSnapshot()
        {
            ConfigurationClient client = _fixture.Client;
            string snapshotName = $"{_snapshotPrefix}archive";

            await CreateReadySnapshot(snapshotName, "archive");
            var response = await client.ArchiveSnapshotAsync(snapshotName);

            Assert.Equal(ConfigurationSnapshotStatus.Archived, response.Value.Status);
        }

        [Fact]
        public async Task RecoverSnapshot()
        {
            ConfigurationClient client = _fixture.Client;
            string snapshotName = $"{_snapshotPrefix}recover";

            await CreateReadySnapshot(snapshotName, "recover");
            await client.ArchiveSnapshotAsync(snapshotName);

            var response = await client.RecoverSnapshotAsync(snapshotName);

            Assert.Equal(ConfigurationSnapshotStatus.Ready, response.Value.Status);
        }

        private async Task<ConfigurationSnapshot> CreateReadySnapshot(string snapshotName, string keySuffix)
        {
            ConfigurationClient client = _fixture.Client;
            string key = $"{_keyPrefix}{keySuffix}";

            await client.SetConfigurationSettingAsync(key, "snapshot-value");

            var snapshot = new ConfigurationSnapshot(new[] { new ConfigurationSettingsFilter(key) })
            {
                SnapshotComposition = SnapshotComposition.Key
            };
            snapshot.Tags["source"] = "client-sdk-test";

            CreateSnapshotOperation operation = await client.CreateSnapshotAsync(
                WaitUntil.Completed,
                snapshotName,
                snapshot);

            return operation.Value;
        }
    }
}