using Xunit;

namespace Azure.AppConfiguration.Emulator.Host.Tests
{
    [Collection("TestServerCollection")]
    public class KeyTests
    {
        private readonly ITestServer _testServer;

        public KeyTests(TestServerFixture fixture)
        {
            _testServer = fixture.TestServer;
        }

        [Fact]
        public async Task GetKeys_ReturnsDistinctKeys()
        {
            // Arrange - Create test key-values
            var client = _testServer.Client;
            var response1 = await TestHelpers.CreateKeyValue(
                client,
                key: "test-key1",
                value: "value1",
                tags: new Dictionary<string, string> { { "tag1", "value1" } });
            response1.EnsureSuccessStatusCode();

            var response2 = await TestHelpers.CreateKeyValue(
                client,
                key: "test-key2",
                value: "value2",
                tags: new Dictionary<string, string> { { "tag2", "value2" } });
            response2.EnsureSuccessStatusCode();

            var response3 = await TestHelpers.CreateKeyValue(
                client,
                key: "multi-label-key",
                value: "value3-dev",
                label: "dev");
            response3.EnsureSuccessStatusCode();

            var response4 = await TestHelpers.CreateKeyValue(
                client,
                key: "multi-label-key",
                value: "value3-prod",
                label: "prod");
            response4.EnsureSuccessStatusCode();

            var result = await TestHelpers.GetKeys(client);

            Assert.NotNull(result);
            Assert.NotNull(result.Items);
            Assert.NotEmpty(result.Items);

            var testKey1 = result.Items.Find(k => k.Name == "test-key1");
            Assert.NotNull(testKey1);

            var testKey2 = result.Items.Find(k => k.Name == "test-key2");
            Assert.NotNull(testKey2);

            var multiLabelKey = result.Items.Find(k => k.Name == "multi-label-key");
            Assert.NotNull(multiLabelKey);
        }

        [Fact]
        public async Task GetKeys_WithNameFilter_ReturnsExactMatchingKey()
        {
            // Arrange
            var client = _testServer.Client;

            // Create test keys
            var response1 = await TestHelpers.CreateKeyValue(
                client,
                key: "test-key1",
                value: "value1",
                tags: new Dictionary<string, string> { { "tag1", "value1" } });
            response1.EnsureSuccessStatusCode();

            var response2 = await TestHelpers.CreateKeyValue(
                client,
                key: "test-key2",
                value: "value2",
                tags: new Dictionary<string, string> { { "tag2", "value2" } });
            response2.EnsureSuccessStatusCode();

            var response3 = await TestHelpers.CreateKeyValue(
                client,
                key: "filtered-key",
                value: "filtered-value",
                tags: new Dictionary<string, string> { { "filtered", "true" } });
            response3.EnsureSuccessStatusCode();

            // Act - Get keys with name filter
            var result = await TestHelpers.GetKeys(client, nameFilter: "filtered-key");

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Items);
            Assert.NotEmpty(result.Items);

            Assert.Single(result.Items);

            var key = result.Items[0];
            Assert.Equal("filtered-key", key.Name);

            Assert.DoesNotContain(result.Items, k => k.Name == "test-key1");
            Assert.DoesNotContain(result.Items, k => k.Name == "test-key2");
        }

        [Fact]
        public async Task GetKeys_WithWildcardNameFilter_ReturnsMatchingKeys()
        {
            // Arrange
            var client = _testServer.Client;

            // Create test keys
            var response1 = await TestHelpers.CreateKeyValue(client, "prefix-key1", "prefix-value1");
            response1.EnsureSuccessStatusCode();

            var response2 = await TestHelpers.CreateKeyValue(client, "prefix-key2", "prefix-value2");
            response2.EnsureSuccessStatusCode();

            var response3 = await TestHelpers.CreateKeyValue(client, "other-key", "other-value");
            response3.EnsureSuccessStatusCode();

            // Act - Get keys with wildcard name filter
            var result = await TestHelpers.GetKeys(client, nameFilter: "prefix*");

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Items);
            Assert.NotEmpty(result.Items);

            Assert.Equal(2, result.Items.Count);

            Assert.All(result.Items, key => Assert.StartsWith("prefix", key.Name));

            Assert.Contains(result.Items, k => k.Name == "prefix-key1");
            Assert.Contains(result.Items, k => k.Name == "prefix-key2");

            Assert.DoesNotContain(result.Items, k => k.Name == "other-key");
        }

        [Fact]
        public async Task GetKeys_WithMultipleNameFilter_ReturnsMatchingKeys()
        {
            // Arrange
            var client = _testServer.Client;

            // Create test keys
            var response1 = await TestHelpers.CreateKeyValue(client, "first-key", "first-value");
            response1.EnsureSuccessStatusCode();

            var response2 = await TestHelpers.CreateKeyValue(client, "second-key", "second-value");
            response2.EnsureSuccessStatusCode();

            var response3 = await TestHelpers.CreateKeyValue(client, "third-key", "third-value");
            response3.EnsureSuccessStatusCode();

            // Act - Get keys with multiple name filter (comma-separated)
            var result = await TestHelpers.GetKeys(client, nameFilter: "first-key,third-key");

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Items);
            Assert.NotEmpty(result.Items);

            Assert.Equal(2, result.Items.Count);

            Assert.Contains(result.Items, k => k.Name == "first-key");
            Assert.Contains(result.Items, k => k.Name == "third-key");

            Assert.DoesNotContain(result.Items, k => k.Name == "second-key");
        }

        [Fact]
        public async Task GetKeys_WithAsteriskNameFilter_ReturnsAllKeys()
        {
            // Arrange
            var client = _testServer.Client;

            // Create test key
            var response = await TestHelpers.CreateKeyValue(client, "asterisk-test-key", "test-asterisk-value");
            response.EnsureSuccessStatusCode();

            // Act - Get keys with asterisk wildcard
            var result = await TestHelpers.GetKeys(client, nameFilter: "*");

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Items);
            Assert.NotEmpty(result.Items);

            Assert.Contains(result.Items, k => k.Name == "asterisk-test-key");
        }
    }
}
