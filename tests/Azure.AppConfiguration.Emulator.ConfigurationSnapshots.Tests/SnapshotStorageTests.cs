using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Moq;

namespace Azure.AppConfiguration.Emulator.ConfigurationSnapshots.Tests
{
    public class SnapshotStorageTests : IDisposable
    {
        private readonly string _temporaryMetadataPath;
        private readonly string _temporaryContentDir;
        private readonly Mock<IHostingEnvironment> _mockEnvironment;

        public SnapshotStorageTests()
        {
            // Set up paths
            var testDir = Path.GetDirectoryName(typeof(SnapshotStorageTests).Assembly.Location) ?? Directory.GetCurrentDirectory();
            _temporaryMetadataPath = Path.Combine(Path.GetTempPath(), $"snap_meta_{Guid.NewGuid()}.ndjson");
            _temporaryContentDir = Path.Combine(Path.GetTempPath(), $"snap_content_{Guid.NewGuid()}");

            // Set up mock environment
            _mockEnvironment = new Mock<IHostingEnvironment>();
            _mockEnvironment.Setup(e => e.ContentRootPath).Returns(testDir);
        }

        public void Dispose()
        {
            // Clean up any temporary files
            if (File.Exists(_temporaryMetadataPath))
            {
                File.Delete(_temporaryMetadataPath);
            }

            // Clean up temp file variations
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
        public async Task AddSnapshot_ThenQuery_ReturnsSnapshot()
        {
            // Arrange
            var providerOptions = new SnapshotProviderOptions();
            var storageOptions = new SnapshotsStorageOptions
            {
                MetadataFilePath = _temporaryMetadataPath,
                ContentDirectory = _temporaryContentDir
            };
            var storage = new SnapshotsStorage(
                Options.Create(providerOptions),
                Options.Create(storageOptions),
                _mockEnvironment.Object);

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
                StatusCode = 0
            };

            // Act
            await storage.AddSnapshot(snapshot, CancellationToken.None);
            var results = await storage.QuerySnapshots().ToListAsync();

            // Assert
            Assert.Single(results);
            Assert.Equal("snap1", results[0].Id);
            Assert.Equal("etag1", results[0].Etag);
            Assert.Equal(SnapshotStatus.Provisioning, results[0].Status);
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
