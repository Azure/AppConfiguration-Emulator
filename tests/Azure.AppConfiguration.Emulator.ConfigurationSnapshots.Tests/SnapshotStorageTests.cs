using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Moq;

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
        public async Task AddSnapshot()
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

            var contentsStorage = new SnapshotContentsStorage(Options.Create(storageOptions), _env.Object);
            var storage = new SnapshotsStorage(
                Options.Create(providerOptions),
                Options.Create(storageOptions),
                _env.Object);

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

            List<Snapshot> initial = await storage.QuerySnapshots().ToListAsync();
            Snapshot storedInitial = initial.Single();
            Assert.Equal("snap1", storedInitial.Id);
            Assert.Equal(SnapshotStatus.Provisioning, storedInitial.Status);
            Assert.Equal(0, storedInitial.ItemCount);
            Assert.Null(storedInitial.Media);

            var items = (await kvProvider.QueryKeyValues(new KeyValueSearchOptions
            {
                KeyFilter = new StringFilter { EqualsTo = "k1" },
                LabelFilter = new StringFilter { EqualsTo = "l1" }
            }, CancellationToken.None)).ToList();

            Directory.CreateDirectory(_temporaryContentDir);
            string filePath = Path.Combine(_temporaryContentDir, snapshot.Id + ".ndjson");
            MediaInfo media = await contentsStorage.CreateContent(filePath, items, CancellationToken.None);

            snapshot.Media = media;
            snapshot.ItemCount = media.Size;
            snapshot.Size = new FileInfo(filePath).Length;
            snapshot.Status = SnapshotStatus.Ready;
            snapshot.LastModified = DateTimeOffset.UtcNow;
            await storage.UpdateSnapshot(snapshot, CancellationToken.None);

            Snapshot persisted = (await storage.QuerySnapshots().ToListAsync()).Single(x => x.Id == "snap1");
            Assert.Equal(SnapshotStatus.Ready, persisted.Status);
            Assert.Equal(1, persisted.ItemCount);
            Assert.NotNull(persisted.Media);
            Assert.Equal(1, persisted.Media.Size);

            List<KeyValue> content = await contentsStorage.GetContent(persisted.Media, 0, CancellationToken.None).ToListAsync();
            Assert.Single(content);
            Assert.Equal("k1", content[0].Key);
            Assert.Equal("l1", content[0].Label);
            Assert.Equal("v1", content[0].Value);
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
