using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.ConfigurationSnapshots.Tests
{
    public class SnapshotsStorageTests : IDisposable
    {
        private readonly string _metadataPath;
        private readonly Mock<IHostingEnvironment> _env;
        private readonly SnapshotsStorage _storage;

        public SnapshotsStorageTests()
        {
            var testDir = Path.GetDirectoryName(typeof(SnapshotsStorageTests).Assembly.Location) ?? Directory.GetCurrentDirectory();
            _metadataPath = Path.Combine(Path.GetTempPath(), $"snap_meta_unit_{Guid.NewGuid()}.ndjson");
            _env = new Mock<IHostingEnvironment>();
            _env.Setup(e => e.ContentRootPath).Returns(testDir);

            var storageOptions = new SnapshotsStorageOptions
            {
                MetadataFilePath = _metadataPath,
                ContentDirectory = Path.Combine(Path.GetTempPath(), $"snap_content_unit_{Guid.NewGuid()}")
            };
            var providerOptions = new SnapshotProviderOptions();

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
            var snapshot = NewProvisioning("snap-add-1");
            await _storage.AddSnapshot(snapshot, CancellationToken.None);

            var list = await _storage.QuerySnapshots().ToListAsync();

            Assert.Single(list);
            Assert.Equal("snap-add-1", list[0].Id);
            Assert.Equal(SnapshotStatus.Provisioning, list[0].Status);
        }

        [Fact]
        public async Task UpdateSnapshot_ReplacesExistingEntry()
        {
            var snapshot = NewProvisioning("snap-upd-1");
            await _storage.AddSnapshot(snapshot, CancellationToken.None);

            snapshot.Status = SnapshotStatus.Ready;
            snapshot.Etag = $"etag-{Guid.NewGuid():N}";
            snapshot.LastModified = DateTimeOffset.UtcNow;
            snapshot.ItemCount = 5;
            snapshot.Size = 1234;
            await _storage.UpdateSnapshot(snapshot, CancellationToken.None);

            var list = await _storage.QuerySnapshots().ToListAsync();
            var stored = list.Single(s => s.Id == snapshot.Id);

            Assert.Equal(SnapshotStatus.Ready, stored.Status);
            Assert.Equal(5, stored.ItemCount);
            Assert.Equal(1234, stored.Size);
        }

        [Fact]
        public async Task QuerySnapshots_ReturnsAll()
        {
            var a = NewProvisioning("snap-q-1");
            var b = NewProvisioning("snap-q-2");
            await _storage.AddSnapshot(a, CancellationToken.None);
            await _storage.AddSnapshot(b, CancellationToken.None);

            var ids = await _storage.QuerySnapshots().Select(s => s.Id).ToListAsync();

            Assert.Contains("snap-q-1", ids);
            Assert.Contains("snap-q-2", ids);
            Assert.Equal(2, ids.Count);
        }

        private static Snapshot NewProvisioning(string id) => new Snapshot
        {
            Id = id,
            Name = id,
            Etag = $"etag-{Guid.NewGuid():N}",
            Status = SnapshotStatus.Provisioning,
            CompositionType = CompositionType.Key,
            RetentionPeriod = TimeSpan.FromMinutes(30),
            Created = DateTimeOffset.UtcNow,
            LastModified = DateTimeOffset.UtcNow,
            Filters = new[] { new KeyValueFilter { Key = "k", Label = "l" } }
        };
    }
}
