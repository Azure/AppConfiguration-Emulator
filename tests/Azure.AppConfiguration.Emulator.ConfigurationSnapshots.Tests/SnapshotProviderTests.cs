using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using Azure.AppConfiguration.Emulator.Search;
using Azure.AppConfiguration.Emulator.Tenant;
using Microsoft.Extensions.Options;
using Moq;

using KvPage = Azure.AppConfiguration.Emulator.ConfigurationSettings.Page<Azure.AppConfiguration.Emulator.ConfigurationSettings.KeyValue>;

namespace Azure.AppConfiguration.Emulator.ConfigurationSnapshots.Tests
{
    public class SnapshotProviderTests
    {
        [Fact]
        public async Task Get_SetsContinuationToken_WhenPageSizeExceeded()
        {
            var testSnapshots = new List<Snapshot>
            {
                NewReadySnapshot("snapshot1"),
                NewReadySnapshot("snapshot2"),
                NewReadySnapshot("snapshot3")
            };

            var mockStorage = new Mock<ISnapshotsStorage>();
            mockStorage.Setup(s => s.QuerySnapshots(It.IsAny<CancellationToken>()))
                .Returns(testSnapshots.ToAsyncEnumerable());

            var providerOptions = new SnapshotProviderOptions
            {
                OutputPageSize = 1 // Set small page size to test pagination
            };

            var provider = new SnapshotProvider(
                mockStorage.Object,
                Mock.Of<ISnapshotContentsStorage>(),
                Mock.Of<IKeyValueProvider>(),
                Options.Create(new TenantOptions()),
                Options.Create(providerOptions));

            await provider.StartAsync(CancellationToken.None);

            IEnumerable<Snapshot> result = await provider.Get(
                new SnapshotSearchOptions { Status = SnapshotStatusSearch.All },
                CancellationToken.None);

            // PaginationFilter surfaces the token via (ObjectResult).Value as IPage, so we assert the same way.
            IPage? page = result as IPage;

            Assert.Equal(1, providerOptions.OutputPageSize);
            Assert.NotNull(page);
            Assert.NotNull(page!.ContinuationToken);
            Assert.Contains(result.First().Name, page.ContinuationToken);
        }

        [Fact]
        public async Task Get_ReturnsSnapshotMatchingName()
        {
            Mock<ISnapshotsStorage> storage = StorageWith(
                NewReadySnapshot("snapshot1"),
                NewReadySnapshot("snapshot2"));

            SnapshotProvider provider = CreateProvider(storage);
            await provider.StartAsync(CancellationToken.None);

            IEnumerable<Snapshot> result = await provider.Get(
                new SnapshotSearchOptions
                {
                    NameFilter = new StringFilter { EqualsTo = "snapshot2" },
                    Status = SnapshotStatusSearch.All
                },
                CancellationToken.None);

            Snapshot single = Assert.Single(result);
            Assert.Equal("snapshot2", single.Name);
        }

        [Fact]
        public async Task Get_ReturnsSnapshotsMatchingNamePrefix()
        {
            Mock<ISnapshotsStorage> storage = StorageWith(
                NewReadySnapshot("app-snapshot1"),
                NewReadySnapshot("app-snapshot2"),
                NewReadySnapshot("other-snapshot"));

            SnapshotProvider provider = CreateProvider(storage);
            await provider.StartAsync(CancellationToken.None);

            IEnumerable<Snapshot> result = await provider.Get(
                new SnapshotSearchOptions
                {
                    NameFilter = new StringFilter { Prefix = "app-" },
                    Status = SnapshotStatusSearch.All
                },
                CancellationToken.None);

            Assert.Equal(2, result.Count());
            Assert.All(result, snapshot => Assert.StartsWith("app-", snapshot.Name));
        }

        [Fact]
        public async Task Get_WithNoneStatus_ThrowsArgumentException()
        {
            SnapshotProvider provider = CreateProvider(StorageWith());

            await Assert.ThrowsAsync<ArgumentException>(() => provider.Get(
                new SnapshotSearchOptions { Status = SnapshotStatusSearch.None },
                CancellationToken.None));
        }

        [Fact]
        public async Task Create_AddsSnapshotWithReadyStatus()
        {
            Mock<ISnapshotsStorage> storage = StorageWith();

            var contents = new Mock<ISnapshotContentsStorage>();
            contents.Setup(c => c.CreateContent(It.IsAny<string>(), It.IsAny<IEnumerable<KeyValue>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MediaInfo { Name = "snapshot1.ndjson", Size = 20 });

            var kvProvider = new Mock<IKeyValueProvider>();
            kvProvider.Setup(p => p.QueryKeyValues(It.IsAny<KeyValueSearchOptions>(), It.IsAny<CancellationToken>()))
                .Returns(() => new ValueTask<KvPage>(new KvPage(new List<KeyValue> { new KeyValue { Key = "k1", Value = "v1" } })));

            SnapshotProvider provider = CreateProvider(storage, contents, kvProvider);
            await provider.StartAsync(CancellationToken.None);

            var snapshot = new Snapshot
            {
                Name = "snapshot1",
                CompositionType = CompositionType.Key,
                RetentionPeriod = TimeSpan.FromHours(1),
                Filters = new[] { new KeyValueFilter { Key = "k*", Label = null } }
            };

            await provider.Create(snapshot, CancellationToken.None);

            Snapshot created = (await provider.Get(
                new SnapshotSearchOptions
                {
                    NameFilter = new StringFilter { EqualsTo = "snapshot1" },
                    Status = SnapshotStatusSearch.All
                },
                CancellationToken.None)).Single();

            Assert.Equal(SnapshotStatus.Ready, created.Status);
            Assert.Equal(1L, created.ItemCount);
            storage.Verify(s => s.AddSnapshot(It.IsAny<Snapshot>(), It.IsAny<CancellationToken>()), Times.Once);
            contents.Verify(c => c.CreateContent(It.IsAny<string>(), It.IsAny<IEnumerable<KeyValue>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Create_WithExistingName_ThrowsConflictException()
        {
            Mock<ISnapshotsStorage> storage = StorageWith(NewReadySnapshot("snapshot1"));

            SnapshotProvider provider = CreateProvider(storage);
            await provider.StartAsync(CancellationToken.None);

            var snapshot = new Snapshot
            {
                Name = "snapshot1",
                CompositionType = CompositionType.Key,
                RetentionPeriod = TimeSpan.FromHours(1),
                Filters = new[] { new KeyValueFilter { Key = "k*", Label = null } }
            };

            await Assert.ThrowsAsync<ConflictException>(() => provider.Create(snapshot, CancellationToken.None));
        }

        [Fact]
        public async Task Archive_SetsStatusToArchived()
        {
            Snapshot snapshot = NewReadySnapshot("snapshot1");
            Mock<ISnapshotsStorage> storage = StorageWith(snapshot);

            SnapshotProvider provider = CreateProvider(storage);
            await provider.StartAsync(CancellationToken.None);

            await provider.Archive(snapshot, CancellationToken.None);

            Snapshot updated = (await provider.Get(
                new SnapshotSearchOptions
                {
                    NameFilter = new StringFilter { EqualsTo = "snapshot1" },
                    Status = SnapshotStatusSearch.All
                },
                CancellationToken.None)).Single();

            Assert.Equal(SnapshotStatus.Archived, updated.Status);
            Assert.NotNull(updated.Expires);
            storage.Verify(s => s.UpdateSnapshot(It.IsAny<Snapshot>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Recover_SetsStatusToReady()
        {
            Snapshot snapshot = NewReadySnapshot("snapshot1");
            snapshot.Status = SnapshotStatus.Archived;
            snapshot.Expires = DateTimeOffset.UtcNow.AddHours(1);

            Mock<ISnapshotsStorage> storage = StorageWith(snapshot);

            SnapshotProvider provider = CreateProvider(storage);
            await provider.StartAsync(CancellationToken.None);

            await provider.Recover(snapshot, CancellationToken.None);

            Snapshot updated = (await provider.Get(
                new SnapshotSearchOptions
                {
                    NameFilter = new StringFilter { EqualsTo = "snapshot1" },
                    Status = SnapshotStatusSearch.All
                },
                CancellationToken.None)).Single();

            Assert.Equal(SnapshotStatus.Ready, updated.Status);
            Assert.Null(updated.Expires);
            storage.Verify(s => s.UpdateSnapshot(It.IsAny<Snapshot>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetContent_ReturnsCapturedKeyValues()
        {
            Snapshot snapshot = NewReadySnapshot("snapshot1");
            snapshot.Media = new MediaInfo { Name = "snapshot1.ndjson", Size = 100 };
            snapshot.ItemCount = 2;

            var contents = new Mock<ISnapshotContentsStorage>();
            contents.Setup(c => c.GetContent(It.IsAny<MediaInfo>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .Returns(new List<KeyValue>
                {
                    new KeyValue { Key = "k1", Value = "v1" },
                    new KeyValue { Key = "k2", Value = "v2" }
                }.ToAsyncEnumerable());

            SnapshotProvider provider = CreateProvider(StorageWith(), contents);

            IEnumerable<KeyValue> result = await provider.GetContent(
                snapshot,
                new SnapshotContentSearchOptions { ContinuationToken = null },
                CancellationToken.None);

            Assert.Equal(2, result.Count());
            Assert.Contains(result, kv => kv.Key == "k1");
            Assert.Contains(result, kv => kv.Key == "k2");
        }

        private static Mock<ISnapshotsStorage> StorageWith(params Snapshot[] snapshots)
        {
            var mock = new Mock<ISnapshotsStorage>();
            mock.Setup(s => s.QuerySnapshots(It.IsAny<CancellationToken>()))
                .Returns(snapshots.ToAsyncEnumerable());
            return mock;
        }

        private static SnapshotProvider CreateProvider(
            Mock<ISnapshotsStorage> storage,
            Mock<ISnapshotContentsStorage>? contents = null,
            Mock<IKeyValueProvider>? kvProvider = null,
            int outputPageSize = 100)
        {
            return new SnapshotProvider(
                storage.Object,
                (contents ?? new Mock<ISnapshotContentsStorage>()).Object,
                (kvProvider ?? new Mock<IKeyValueProvider>()).Object,
                Options.Create(new TenantOptions()),
                Options.Create(new SnapshotProviderOptions { OutputPageSize = outputPageSize }));
        }

        private static Snapshot NewReadySnapshot(string name)
        {
            return new Snapshot
            {
                Id = name,
                Name = name,
                Etag = $"etag-{Guid.NewGuid():N}",
                Status = SnapshotStatus.Ready,
                CompositionType = CompositionType.Key,
                RetentionPeriod = TimeSpan.FromHours(1),
                Created = DateTimeOffset.UtcNow,
                LastModified = DateTimeOffset.UtcNow,
                Filters = new[] { new KeyValueFilter { Key = "k*", Label = null } }
            };
        }
    }
}
