using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Moq;

namespace Azure.AppConfiguration.Emulator.ConfigurationSnapshots.Tests
{
    public class SnapshotsStorageTests : IDisposable
    {
        private readonly string _metadataPath;
        private readonly string _contentDir;
        private readonly Mock<IHostingEnvironment> _env;
        private readonly SnapshotsStorage _storage;

        public SnapshotsStorageTests()
        {
            var testDir = Path.GetDirectoryName(typeof(SnapshotsStorageTests).Assembly.Location) ?? Directory.GetCurrentDirectory();
            _metadataPath = Path.Combine(Path.GetTempPath(), $"snap_meta_unit_{Guid.NewGuid()}.ndjson");
            _contentDir = Path.Combine(Path.GetTempPath(), $"snap_content_unit_{Guid.NewGuid()}");
            _env = new Mock<IHostingEnvironment>();
            _env.Setup(e => e.ContentRootPath).Returns(testDir);

            var storageOptions = new SnapshotsStorageOptions
            {
                MetadataFilePath = _metadataPath,
                ContentDirectory = _contentDir,
                WriteBufferSize = 8192,
                AppendBufferSize = 4096,
                ReadBufferSizeHint = 4096,
                MaxReadBufferSize = 65536,
                ReadTimeout = TimeSpan.FromSeconds(5),
                WriteTimeout = TimeSpan.FromSeconds(5)
            };
            var providerOptions = new SnapshotProviderOptions
            {
                OutputPageSize = 100,
                ReadTimeout = TimeSpan.FromSeconds(5),
                WriteTimeout = TimeSpan.FromSeconds(5),
                RetryTimeout = TimeSpan.FromSeconds(5),
                ConflictRetryTimeout = TimeSpan.FromSeconds(5),
                MinFilterCount = 1,
                MaxFilterCount = 100
            };

            _storage = new SnapshotsStorage(
                Options.Create(providerOptions),
                Options.Create(storageOptions),
                _env.Object);
        }

        public void Dispose()
        {
            if (File.Exists(_metadataPath))
            {
                File.Delete(_metadataPath);
            }

            if (Directory.Exists(_contentDir))
            {
                Directory.Delete(_contentDir, true);
            }

            var tmp = _metadataPath + ".tmp";
            if (File.Exists(tmp))
            {
                File.Delete(tmp);
            }

            var bak = _metadataPath + ".bac";
            if (File.Exists(bak))
            {
                File.Delete(bak);
            }
        }

        [Fact]
        public async Task AddSnapshot_PersistsEntry()
        {
            var snapshot = NewSnapshot("snap-add-1");
            await _storage.AddSnapshot(snapshot, CancellationToken.None);

            var list = await _storage.QuerySnapshots().ToListAsync();

            Assert.Single(list);
            Assert.Equal("snap-add-1", list[0].Id);
            Assert.Equal(SnapshotStatus.Ready, list[0].Status);
        }

        [Fact]
        public async Task UpdateSnapshot_ReplacesExistingEntry()
        {
            var snapshot = NewSnapshot("snap-upd-1");
            await _storage.AddSnapshot(snapshot, CancellationToken.None);

            snapshot.ItemCount = 5;
            snapshot.Size = 1234;
            snapshot.Etag = $"etag-{Guid.NewGuid():N}";
            snapshot.LastModified = DateTimeOffset.UtcNow;
            await _storage.UpdateSnapshot(snapshot, CancellationToken.None);

            var list = await _storage.QuerySnapshots().ToListAsync();
            var stored = list.Single(s => s.Id == snapshot.Id);

            Assert.Equal(5, stored.ItemCount);
            Assert.Equal(1234, stored.Size);
        }

        [Fact]
        public async Task QuerySnapshots_ReturnsAll()
        {
            var a = NewSnapshot("snap-q-1");
            var b = NewSnapshot("snap-q-2");
            await _storage.AddSnapshot(a, CancellationToken.None);
            await _storage.AddSnapshot(b, CancellationToken.None);

            var ids = await _storage.QuerySnapshots().Select(s => s.Id).ToListAsync();

            Assert.Contains("snap-q-1", ids);
            Assert.Contains("snap-q-2", ids);
            Assert.Equal(2, ids.Count);
        }

        [Fact]
        public async Task CreateContent_WritesFile_AndReturnsMediaInfo()
        {
            Directory.CreateDirectory(_contentDir);
            string fileName = "snapA.ndjson";
            string filePath = Path.Combine(_contentDir, fileName);
            var items = new List<KeyValue>
            {
                new KeyValue { Key = "k1", Label = "l1", Value = "v1" },
                new KeyValue { Key = "k2", Label = "l2", Value = "v2" }
            };

            MediaInfo media = await _storage.CreateContent(fileName, items, CancellationToken.None);

            Assert.NotNull(media);
            Assert.Equal("snapshots", media.Category);
            Assert.Equal("application/x-ndjson", media.ContentType);
            Assert.Equal(fileName, media.Name);
            Assert.True(File.Exists(filePath));
            long physicalSize = new FileInfo(filePath).Length;
            Assert.Equal(physicalSize, media.Size);
            Assert.False(string.IsNullOrEmpty(media.Etag));
            Assert.NotNull(media.Sha256Hash);
        }

        [Fact]
        public async Task GetContent_ReturnsItems_FromOffset()
        {
            Directory.CreateDirectory(_contentDir);
            string fileName = "snapB.ndjson";
            var items = new List<KeyValue>
            {
                new KeyValue { Key = "k1", Label = "l1", Value = "v1" },
                new KeyValue { Key = "k2", Label = "l2", Value = "v2" },
                new KeyValue { Key = "k3", Label = "l3", Value = "v3" }
            };

            MediaInfo media = await _storage.CreateContent(fileName, items, CancellationToken.None);

            var fromSecond = new List<KeyValue>();
            await foreach (var kv in _storage.GetContent(media, 1, CancellationToken.None))
            {
                fromSecond.Add(kv);
            }

            Assert.Equal(2, fromSecond.Count);
            Assert.Equal("k2", fromSecond[0].Key);
            Assert.Equal("k3", fromSecond[1].Key);
        }

        [Fact]
        public async Task GetContent_WithInvalidMedia_ReturnsEmpty()
        {
            var empty = new List<KeyValue>();
            await foreach (var kv in _storage.GetContent(new MediaInfo { Name = null }, 0, CancellationToken.None))
            {
                empty.Add(kv);
            }

            Assert.Empty(empty);
        }

        [Fact]
        public async Task RemoveSnapshots_RemovesMetadataAndDeletesContent()
        {
            Directory.CreateDirectory(_contentDir);

            var snapA = NewSnapshot("snapA");
            snapA.Media = await _storage.CreateContent("snapA.ndjson", new[] { new KeyValue { Key = "k1", Label = "l1", Value = "v1" } }, CancellationToken.None);
            snapA.ItemCount = 1;
            snapA.Size = snapA.Media.Size;

            var snapB = NewSnapshot("snapB");
            snapB.Media = await _storage.CreateContent("snapB.ndjson", new[] { new KeyValue { Key = "k2", Label = "l2", Value = "v2" } }, CancellationToken.None);
            snapB.ItemCount = 1;
            snapB.Size = snapB.Media.Size;

            await _storage.AddSnapshot(snapA, CancellationToken.None);
            await _storage.AddSnapshot(snapB, CancellationToken.None);

            Assert.True(File.Exists(Path.Combine(_contentDir, snapA.Media.Name)));
            Assert.True(File.Exists(Path.Combine(_contentDir, snapB.Media.Name)));

            await _storage.RemoveSnapshots(new[] { snapA }, CancellationToken.None);

            var all = await _storage.QuerySnapshots().ToListAsync();
            Assert.DoesNotContain(all, s => s.Id == "snapA");
            Assert.Contains(all, s => s.Id == "snapB");

            Assert.False(File.Exists(Path.Combine(_contentDir, snapA.Media.Name)));
            Assert.True(File.Exists(Path.Combine(_contentDir, snapB.Media.Name)));
        }

        private static Snapshot NewSnapshot(string id) => new Snapshot
        {
            Id = id,
            Name = id,
            Etag = $"etag-{Guid.NewGuid():N}",
            Status = SnapshotStatus.Ready,
            CompositionType = CompositionType.Key,
            RetentionPeriod = TimeSpan.FromMinutes(30),
            Created = DateTimeOffset.UtcNow,
            LastModified = DateTimeOffset.UtcNow,
            Filters = new[] { new KeyValueFilter { Key = "k", Label = "l" } }
        };
    }
}
