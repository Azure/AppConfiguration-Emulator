using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;

namespace Azure.AppConfiguration.Emulator.ConfigurationSnapshots.Tests
{
    // Lightweight stub just returns a preset list of key-values in a single page
    internal sealed class StubKeyValueProvider : IKeyValueProvider
    {
        private readonly List<KeyValue> _items;
        public StubKeyValueProvider(IEnumerable<KeyValue> items) => _items = items.ToList();
        public ValueTask<ConfigurationSettings.Page<KeyValue>> QueryKeyValues(KeyValueSearchOptions options, CancellationToken cancellationToken)
            => new(new ConfigurationSettings.Page<KeyValue>(_items));
        public ValueTask<KeyValue> GetKeyValue(string key, string label, CancellationToken cancellationToken) => new(new KeyValue());
        public ValueTask<KeyValue> Set(KeyValue kv, CancellationToken cancellationToken) => new(new KeyValue());
        public ValueTask Remove(KeyValue kv, CancellationToken cancellationToken) => ValueTask.CompletedTask;
        public ValueTask<KeyValue> Lock(KeyValue kv, CancellationToken cancellationToken) => new(new KeyValue());
        public ValueTask<KeyValue> Unlock(KeyValue kv, CancellationToken cancellationToken) => new(new KeyValue());
    }

    public class SnapshotStorageTests : IDisposable
    {
        private readonly string _temporaryMetadataPath;
        private readonly string _temporaryContentDir;
        private readonly Mock<IHostingEnvironment> _env;

        public SnapshotStorageTests()
        {
            var testDir = Path.GetDirectoryName(typeof(SnapshotStorageTests).Assembly.Location) ?? Directory.GetCurrentDirectory();
            _temporaryMetadataPath = Path.Combine(Path.GetTempPath(), $"snap_meta_{Guid.NewGuid()}.ndjson");
            _temporaryContentDir = Path.Combine(Path.GetTempPath(), $"snap_content_{Guid.NewGuid()}");
            _env = new Mock<IHostingEnvironment>();
            _env.Setup(m => m.ContentRootPath).Returns(testDir);
        }

        public void Dispose()
        {
            if (File.Exists(_temporaryMetadataPath))
            {
                File.Delete(_temporaryMetadataPath);
            }

            var tmp = _temporaryMetadataPath + ".tmp";
            if (File.Exists(tmp))
            {
                File.Delete(tmp);
            }

            var bak = _temporaryMetadataPath + ".bac";
            if (File.Exists(bak))
            {
                File.Delete(bak);
            }

            if (Directory.Exists(_temporaryContentDir))
            {
                Directory.Delete(_temporaryContentDir, recursive: true);
            }
        }

        [Fact]
        public async Task AddSnapshots()
        {
            var providerOptions = new SnapshotProviderOptions();
            var storageOptions = new SnapshotsStorageOptions
            {
                MetadataFilePath = _temporaryMetadataPath,
                ContentDirectory = _temporaryContentDir
            };

            var returnedKv = new KeyValue
            {
                Key = "k1",
                Label = "l1",
                Value = "v1",
                Etag = "e1",
                Timestamp = DateTimeOffset.UtcNow
            };
            var kvProvider = new StubKeyValueProvider(new[] { returnedKv });

            var storage = new SnapshotsStorage(
                Options.Create(providerOptions),
                Options.Create(storageOptions),
                _env.Object);

            var manager = new SnapshotsManager(storage, kvProvider, NullLogger<SnapshotsManager>.Instance);

            var snapshot = new Snapshot
            {
                Id = "snap1",
                Name = "snap1",
                Etag = "etag1",
                Status = SnapshotStatus.Provisioning,
                CompositionType = CompositionType.Key,
                RetentionPeriod = TimeSpan.FromMinutes(30),
                Created = DateTimeOffset.UtcNow,
                LastModified = DateTimeOffset.UtcNow,
                ItemCount = 0,
                Size = 0,
                StatusCode = 0,
                Filters = new[] { new KeyValueFilter { Key = "k1", Label = "l1" } }
            };

            await storage.AddSnapshot(snapshot, CancellationToken.None);

            var initial = await storage.QuerySnapshots().ToListAsync();
            var storedInitial = initial.Single();
            Assert.Equal("snap1", storedInitial.Id);
            Assert.Equal(SnapshotStatus.Provisioning, storedInitial.Status);
            Assert.Equal(0, storedInitial.ItemCount);
            Assert.Null(storedInitial.Media);

            // Provision via manager
            await manager.Provision(snapshot, CancellationToken.None);

            var after = await storage.QuerySnapshots().ToListAsync();
            var readySnapshot = after.Single();
            Assert.Equal(SnapshotStatus.Ready, readySnapshot.Status);
            Assert.Equal(1, readySnapshot.ItemCount);
            Assert.NotNull(readySnapshot.Media);
            Assert.Equal(1, readySnapshot.Media.Size);

            // Verify content
            var content = await storage.ReadSnapshotContent(readySnapshot, 0).ToListAsync();
            Assert.Single(content);
            Assert.Equal("k1", content[0].Key);
            Assert.Equal("l1", content[0].Label);
            Assert.Equal("v1", content[0].Value);
        }

        [Fact]
        public async Task UpdateSnapshot()
        {
            var providerOptions = new SnapshotProviderOptions();
            var storageOptions = new SnapshotsStorageOptions
            {
                MetadataFilePath = _temporaryMetadataPath,
                ContentDirectory = _temporaryContentDir
            };
            var returnedKv = new KeyValue
            {
                Key = "k1",
                Label = "l1",
                Value = "v1",
                Etag = "e1",
                Timestamp = DateTimeOffset.UtcNow
            };
            var kvProvider = new StubKeyValueProvider(new[] { returnedKv });
            var storage = new SnapshotsStorage(
                Options.Create(providerOptions),
                Options.Create(storageOptions),
                _env.Object);
            var manager = new SnapshotsManager(storage, kvProvider, NullLogger<SnapshotsManager>.Instance);
            var snapshot = new Snapshot
            {
                Id = "snap2",
                Name = "snap2",
                Etag = "etag-init",
                Status = SnapshotStatus.Provisioning,
                CompositionType = CompositionType.Key,
                RetentionPeriod = TimeSpan.FromMinutes(30),
                Created = DateTimeOffset.UtcNow,
                LastModified = DateTimeOffset.UtcNow,
                Filters = new[] { new KeyValueFilter { Key = "k1", Label = "l1" } }
            };
            await storage.AddSnapshot(snapshot, CancellationToken.None);

            // Provision
            await manager.Provision(snapshot, CancellationToken.None);
            var ready = (await storage.QuerySnapshots().ToListAsync()).Single(x => x.Id == "snap2");
            Assert.Equal(SnapshotStatus.Ready, ready.Status);

            // Update status to Archived and change Etag
            ready.Etag = "etag-archived";
            ready.Status = SnapshotStatus.Archived;
            ready.LastModified = DateTimeOffset.UtcNow;
            await storage.UpdateSnapshot(ready, CancellationToken.None);

            // Fetch again
            var updated = (await storage.QuerySnapshots().ToListAsync()).Single(x => x.Id == "snap2");
            Assert.Equal(SnapshotStatus.Archived, updated.Status);
            Assert.Equal("etag-archived", updated.Etag);

            // Content access should yield no items
            var content = await storage.ReadSnapshotContent(updated, 0).ToListAsync();
            Assert.Empty(content);
        }
    }

    internal static class AsyncEnumerableExtensions
    {
        public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> source)
        {
            var list = new List<T>();
            await foreach (var item in source)
            {
                list.Add(item);
            }

            return list;
        }
    }
}
