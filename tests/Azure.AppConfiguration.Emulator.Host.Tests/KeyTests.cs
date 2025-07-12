using System.Net;
using System.Text.Json;
using Xunit;

namespace Azure.AppConfiguration.Emulator.Host.Tests
{
    [Collection("TestServerCollection")]
    public class KeyTests
    {
        private readonly TestServer _testServer;

        public KeyTests(TestServerFixture fixture)
        {
            _testServer = fixture.TestServer;
        }

        [Fact]
        public async Task GetKeys_ReturnsDistinctKeys()
        {
            // Arrange
            var client = _testServer.ServerClient;

            // Act
            var response = await client.GetAsync("/keys");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            // Parse the JSON response
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var result = JsonSerializer.Deserialize<KeysResponse>(content, options);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Items);
            Assert.NotEmpty(result.Items);

            // Verify the distinct keys are returned
            var testKey1 = result.Items.Find(k => k.Name == "test-key1");
            Assert.NotNull(testKey1);

            var testKey2 = result.Items.Find(k => k.Name == "test-key2");
            Assert.NotNull(testKey2);

            var multiLabelKey = result.Items.Find(k => k.Name == "multi-label-key");
            Assert.NotNull(multiLabelKey);

            // Verify we get distinct keys (no duplicates)
            var uniqueKeys = result.Items.Select(k => k.Name).Distinct().Count();
            Assert.Equal(uniqueKeys, result.Items.Count);
        }

        [Fact]
        public async Task GetKeys_WithNameFilter_ReturnsFilteredKeys()
        {
            // Arrange
            var client = _testServer.ServerClient;
            var nameFilter = "filtered-key";

            // Act
            var response = await client.GetAsync($"/keys?name={nameFilter}*");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            // Parse the JSON response
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var result = JsonSerializer.Deserialize<KeysResponse>(content, options);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Items);
            Assert.NotEmpty(result.Items);
            Assert.All(result.Items, item => Assert.StartsWith(nameFilter, item.Name));
        }

        // Helper class to deserialize the response
        private class KeysResponse
        {
            public List<Key> Items { get; set; }

            public class Key
            {
                public string Name { get; set; }
            }
        }
    }
}
