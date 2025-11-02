using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Moq;

namespace Azure.AppConfiguration.Emulator.ConfigurationSnapshots.Tests
{
    public class SnapshotContentsStorageTests : IDisposable
    {
        private readonly string _contentDir;
        private readonly Mock<IHostingEnvironment> _env;
        private readonly SnapshotContentsStorage _contents;

        public SnapshotContentsStorageTests()
        {
            string testDir = Path.GetDirectoryName(typeof(SnapshotContentsStorageTests).Assembly.Location) ?? Directory.GetCurrentDirectory();
            _contentDir = Path.Combine(Path.GetTempPath(), $"snap_content_unit_{Guid.NewGuid()}");
            _env = new Mock<IHostingEnvironment>();
            _env.Setup(e => e.ContentRootPath).Returns(testDir);

            var options = new SnapshotsStorageOptions
            {
                ContentDirectory = _contentDir,
                WriteBufferSize = 8192,
                AppendBufferSize = 4096,
                ReadBufferSizeHint = 4096,
                MaxReadBufferSize = 65536,
                ReadTimeout = TimeSpan.FromSeconds(5),
                WriteTimeout = TimeSpan.FromSeconds(5)
            };

            _contents = new SnapshotContentsStorage(Options.Create(options), _env.Object);
        }

        public void Dispose()
        {
            if (Directory.Exists(_contentDir))
            {
                Directory.Delete(_contentDir, true);
            }
        }

        [Fact]
        public async Task CreateContent_WritesFile_AndReturnsMediaInfo()
        {
            Directory.CreateDirectory(_contentDir);
            string filePath = Path.Combine(_contentDir, "snapA.ndjson");
            var items = new List<KeyValue>
            {
                new KeyValue { Key = "k1", Label = "l1", Value = "v1" },
                new KeyValue { Key = "k2", Label = "l2", Value = "v2" }
            };

            MediaInfo media = await _contents.CreateContent(filePath, items, CancellationToken.None);

            Assert.NotNull(media);
            Assert.Equal("snapshots", media.Category);
            Assert.Equal("application/x-ndjson", media.ContentType);
            Assert.Equal(Path.GetFileName(filePath), media.Name);
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
            string filePath = Path.Combine(_contentDir, "snapB.ndjson");
            var items = new List<KeyValue>
            {
                new KeyValue { Key = "k1", Label = "l1", Value = "v1" },
                new KeyValue { Key = "k2", Label = "l2", Value = "v2" },
                new KeyValue { Key = "k3", Label = "l3", Value = "v3" }
            };
            MediaInfo media = await _contents.CreateContent(filePath, items, CancellationToken.None);

            var fromSecond = new List<KeyValue>();
            await foreach (var kv in _contents.GetContent(media, 1, CancellationToken.None))
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
            await foreach (var kv in _contents.GetContent(new MediaInfo { Name = null }, 0, CancellationToken.None))
            {
                empty.Add(kv);
            }

            Assert.Empty(empty);
        }
    }
}
