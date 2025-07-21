using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Moq;

namespace Azure.AppConfiguration.Emulator.ConfigurationSettings.Tests
{
    public class KeyValueStorageTests : IDisposable
    {
        private readonly string _testFilePath;
        private readonly string _temporaryFilePath;
        private readonly Mock<IHostingEnvironment> _mockEnvironment;

        public KeyValueStorageTests()
        {
            // Set up paths
            var testDir = Path.GetDirectoryName(typeof(KeyValueStorageTests).Assembly.Location) ?? Directory.GetCurrentDirectory();
            _testFilePath = Path.Combine(testDir, "kv.ndjson");
            _temporaryFilePath = Path.Combine(Path.GetTempPath(), $"test_kv_{Guid.NewGuid()}.ndjson");

            // Set up mock environment
            _mockEnvironment = new Mock<IHostingEnvironment>();
            _mockEnvironment.Setup(m => m.ContentRootPath).Returns(testDir);
        }

        public void Dispose()
        {
            // Clean up any temporary files
            if (File.Exists(_temporaryFilePath))
            {
                File.Delete(_temporaryFilePath);
            }

            // Clean up temp file variations
            if (File.Exists($"{_temporaryFilePath}.tmp"))
            {
                File.Delete($"{_temporaryFilePath}.tmp");
            }

            if (File.Exists($"{_temporaryFilePath}.bac"))
            {
                File.Delete($"{_temporaryFilePath}.bac");
            }
        }

        [Fact]
        public async Task QueryKeyValues_ReadsExistingFile()
        {
            // Arrange
            var options = new KeyValueStorageOptions { FilePath = _testFilePath };
            var storage = new KeyValueStorage(Options.Create(options), _mockEnvironment.Object);

            // Act
            var keyValues = await storage.QueryKeyValues(CancellationToken.None).ToListAsync();

            // Assert
            Assert.Equal(3, keyValues.Count);
            Assert.Contains(keyValues, kv => kv.Key == "key1" && kv.Value == "1");
            Assert.Contains(keyValues, kv => kv.Key == "key2" && kv.Value == "2");
            Assert.Contains(keyValues, kv => kv.Key == "key3" && kv.Value == "3");
        }

        [Fact]
        public async Task AppendKeyValue_CreatesFileAndAppendsValue()
        {
            // Arrange
            var options = new KeyValueStorageOptions { FilePath = _temporaryFilePath };
            var storage = new KeyValueStorage(Options.Create(options), _mockEnvironment.Object);

            var keyValue = new KeyValue
            {
                Key = "newKey",
                Value = "newValue",
                Label = "newLabel",
                Etag = "newEtag",
                Timestamp = DateTimeOffset.UtcNow
            };

            // Act - This should create the file and append the key-value
            await storage.AppendKeyValue(keyValue, CancellationToken.None);

            // Assert
            Assert.True(File.Exists(_temporaryFilePath));

            // Read the file to verify content
            var result = await storage.QueryKeyValues(CancellationToken.None).ToListAsync();
            Assert.Single(result);
            Assert.Equal("newKey", result[0].Key);
            Assert.Equal("newValue", result[0].Value);
            Assert.Equal("newLabel", result[0].Label);
        }

        [Fact]
        public async Task Save_OverwritesFileContent()
        {
            // Arrange
            var options = new KeyValueStorageOptions { FilePath = _temporaryFilePath };
            var storage = new KeyValueStorage(Options.Create(options), _mockEnvironment.Object);

            // First, append a key-value to create the file
            var initialKeyValue = new KeyValue
            {
                Key = "initialKey",
                Value = "initialValue",
                Label = "initialLabel",
                Etag = "initialEtag",
                Timestamp = DateTimeOffset.UtcNow
            };

            await storage.AppendKeyValue(initialKeyValue, CancellationToken.None);

            // Create new key-values to save
            var newKeyValues = new List<KeyValue>
            {
                new KeyValue
                {
                    Key = "saveKey1",
                    Value = "saveValue1",
                    Label = "saveLabel1",
                    Etag = "saveEtag1",
                    Timestamp = DateTimeOffset.UtcNow
                },
                new KeyValue
                {
                    Key = "saveKey2",
                    Value = "saveValue2",
                    Label = "saveLabel2",
                    Etag = "saveEtag2",
                    Timestamp = DateTimeOffset.UtcNow
                }
            };

            // Act - This should overwrite the file
            await storage.Save(newKeyValues, CancellationToken.None);

            // Assert
            var result = await storage.QueryKeyValues(CancellationToken.None).ToListAsync();

            // Should have only the new key-values, not the initial one
            Assert.Equal(2, result.Count);
            Assert.Contains(result, kv => kv.Key == "saveKey1" && kv.Value == "saveValue1");
            Assert.Contains(result, kv => kv.Key == "saveKey2" && kv.Value == "saveValue2");
            Assert.DoesNotContain(result, kv => kv.Key == "initialKey");
        }
    }

    // Extension method to convert IAsyncEnumerable to List
    public static class AsyncEnumerableExtensions
    {
        public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> source)
        {
            var result = new List<T>();

            await foreach (var item in source)
            {
                result.Add(item);
            }

            return result;
        }
    }
}
