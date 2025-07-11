using System.Text.Json;
using Azure.AppConfiguration.Emulator.ConfigurationSettings;

namespace Azure.AppConfiguration.Emulator.Host.Tests
{
    public class KeyValueTests : IDisposable
    {
        private readonly TestServer _testServer;

        public KeyValueTests()
        {
            _testServer = new TestServer();
        }

        [Fact]
        public async Task GetKeyValues_ReturnsPrePopulatedTestData()
        {
            // Arrange
            var client = _testServer.ServerClient;

            // Act
            var response = await client.GetAsync("/kv");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            // Parse the JSON response
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var result = JsonSerializer.Deserialize<KeyValueResponse>(content, options);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Items);
            Assert.NotEmpty(result.Items);

            // Verify test data is returned
            var testKey1 = result.Items.Find(kv => kv.Key == "test-key1" && kv.Label == "test-label1");
            Assert.NotNull(testKey1);
            Assert.Equal("test-value1", testKey1.Value);

            var testKey2 = result.Items.Find(kv => kv.Key == "test-key2" && string.IsNullOrEmpty(kv.Label));
            Assert.NotNull(testKey2);
            Assert.Equal("test-value2", testKey2.Value);
        }

        public void Dispose()
        {
            _testServer?.Dispose();
        }

        // Helper class to deserialize the response
        private class KeyValueResponse
        {
            public List<KeyValue> Items { get; set; }
        }
    }
}
