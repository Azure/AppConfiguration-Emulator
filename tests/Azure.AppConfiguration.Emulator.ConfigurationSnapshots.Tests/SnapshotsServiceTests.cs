using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Moq;
using System.Runtime.CompilerServices;

namespace Azure.AppConfiguration.Emulator.ConfigurationSnapshots.Tests
{
    internal class InMemorySnapshotsStorage : ISnapshotsStorage
    {
        private readonly List<Snapshot> _snapshots = new();

        public IAsyncEnumerable<Snapshot> QuerySnapshots()
        {
            return _snapshots.ToAsyncEnumerable();
        }

        public Task<Snapshot> GetSnapshot(string snapshotId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_snapshots.FirstOrDefault(s => s.Id == snapshotId)!);
        }

        public Task AddSnapshot(Snapshot snapshot, CancellationToken cancellationToken)
        {
            _snapshots.Add(snapshot);
            return Task.CompletedTask;
        }

        public Task UpdateSnapshot(Snapshot snapshot, CancellationToken cancellationToken)
        {
            for (int i = 0; i < _snapshots.Count; i++)
            {
                if (_snapshots[i].Id == snapshot.Id)
                {
                    _snapshots[i] = snapshot;
                    break;
                }
            }

            return Task.CompletedTask;
        }
    }

    internal class InMemorySnapshotContentsStorage : ISnapshotContentsStorage
    {
        private readonly Dictionary<string, List<KeyValue>> _files = new();

        public Task<MediaInfo> CreateContent(string fileName, IEnumerable<KeyValue> items, CancellationToken cancellationToken)
        {
            var list = items.ToList();
            _files[fileName] = list;
            var media = new MediaInfo
            {
                Category = "snapshots",
                ContentType = "application/x-ndjson",
                Name = fileName,
                Size = list.Count,
                Etag = $"etag",
                Sha256Hash = new byte[] { 1, 2, 3 }
            };

            return Task.FromResult(media);
        }

        public async IAsyncEnumerable<KeyValue> GetContent(MediaInfo media, long offset, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (media == null || string.IsNullOrEmpty(media.Name)) yield break;
            string path = _files.Keys.FirstOrDefault(k => Path.GetFileName(k) == media.Name) ?? string.Empty;
            if (string.IsNullOrEmpty(path)) yield break;
            var list = _files[path];
            for (int i = (int)offset; i < list.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return list[i];
                await Task.Yield();
            }
        }
    }

    public class SnapshotsServiceTests
    {
        [Fact]
        public async Task BackgroundService_ProvisioningSnapshot_BecomesReadyWithContent()
        {
            var kvItems = new[]
            {
                new KeyValue { Key = "k1", Label = "l1", Value = "v1" },
                new KeyValue { Key = "k2", Label = "l2", Value = "v2" }
            };

            var storage = new InMemorySnapshotsStorage();
            var contents = new InMemorySnapshotContentsStorage();

            var kvProviderMock = new Mock<IKeyValueProvider>();
            kvProviderMock
                .Setup(m => m.QueryKeyValues(It.IsAny<KeyValueSearchOptions>(), It.IsAny<CancellationToken>()))
                .Returns((KeyValueSearchOptions opts, CancellationToken ct) => new ValueTask<Azure.AppConfiguration.Emulator.ConfigurationSettings.Page<Azure.AppConfiguration.Emulator.ConfigurationSettings.KeyValue>>(new Azure.AppConfiguration.Emulator.ConfigurationSettings.Page<Azure.AppConfiguration.Emulator.ConfigurationSettings.KeyValue>(kvItems)));
            kvProviderMock.Setup(m => m.GetKeyValue(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(new ValueTask<KeyValue>(new KeyValue()));
            kvProviderMock.Setup(m => m.Set(It.IsAny<KeyValue>(), It.IsAny<CancellationToken>())).Returns(new ValueTask<KeyValue>(new KeyValue()));
            kvProviderMock.Setup(m => m.Remove(It.IsAny<KeyValue>(), It.IsAny<CancellationToken>())).Returns(new ValueTask());
            kvProviderMock.Setup(m => m.Lock(It.IsAny<KeyValue>(), It.IsAny<CancellationToken>())).Returns(new ValueTask<KeyValue>(new KeyValue()));
            kvProviderMock.Setup(m => m.Unlock(It.IsAny<KeyValue>(), It.IsAny<CancellationToken>())).Returns(new ValueTask<KeyValue>(new KeyValue()));
            var kvProvider = kvProviderMock.Object;

            var logger = new Mock<ILogger<SnapshotsService>>().Object;

            var snapshot = new Snapshot
            {
                Id = "snap-1",
                Name = "snap-1",
                Etag = $"etag-{Guid.NewGuid():N}",
                Status = SnapshotStatus.Provisioning,
                CompositionType = CompositionType.Key,
                RetentionPeriod = TimeSpan.FromMinutes(5),
                Created = DateTimeOffset.UtcNow,
                LastModified = DateTimeOffset.UtcNow,
                Filters = new[] { new KeyValueFilter { Key = "k1", Label = "l1" }, new KeyValueFilter { Key = "k2", Label = "l2" } }
            };

            await storage.AddSnapshot(snapshot, CancellationToken.None);

            // Create real service
            var service = new SnapshotsService(storage, contents, kvProvider, logger);
            await ((IHostedService)service).StartAsync(CancellationToken.None);

            Snapshot? ready = null;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (sw.Elapsed < TimeSpan.FromSeconds(5))
            {
                ready = await storage.GetSnapshot("snap-1", CancellationToken.None);
                if (ready?.Status == SnapshotStatus.Ready)
                {
                    break;
                }

                await Task.Delay(100);
            }

            await ((IHostedService)service).StopAsync(CancellationToken.None);

            Assert.NotNull(ready);
            var updated = ready!;
            Assert.Equal(SnapshotStatus.Ready, updated.Status);
            Assert.True(updated.Size > 0);
            Assert.NotNull(updated.Media);
            Assert.Equal(updated.Size, updated.Media.Size);
        }
    }
}
