using Xunit;

namespace Azure.AppConfiguration.Emulator.Host.Tests
{
    [Collection("TestServerCollection")]
    public class KeyValueTests
    {
        private readonly ITestServer _testServer;

        public KeyValueTests(TestServerFixture fixture)
        {
            _testServer = fixture.TestServer;
        }

        [Fact]
        public async Task GetKeyValue_ByPath_ReturnsCorrectKeyValue()
        {
            // Arrange - Create a key-value
            var client = _testServer.Client;
            var key = "test-key";
            var value = "test-value";
            var tags = new Dictionary<string, string> { { "env", "test" } };

            // Create the key-value
            var createResponse = await TestHelpers.CreateKeyValue(client, key, value, tags: tags);
            createResponse.EnsureSuccessStatusCode();

            // Act - Get the key-value by path
            var keyValue = await TestHelpers.GetKeyValue(client, key);

            // Assert
            Assert.NotNull(keyValue);
            Assert.Equal(key, keyValue.Key);
            Assert.Equal(value, keyValue.Value);
            Assert.NotNull(keyValue.Tags);
            Assert.Contains(keyValue.Tags, t => t.Key == "env" && t.Value == "test");
        }

        [Fact]
        public async Task GetKeyValue_ByPathWithLabel_ReturnsCorrectKeyValue()
        {
            // Arrange - Create key-values with different labels
            var client = _testServer.Client;
            var key = "multi-label-key";
            var devValue = "dev-value";
            var prodValue = "prod-value";
            var devLabel = "dev";
            var prodLabel = "prod";

            // Create dev key-value
            var devCreateResponse = await TestHelpers.CreateKeyValue(client, key, devValue, label: devLabel);
            devCreateResponse.EnsureSuccessStatusCode();

            // Create prod key-value
            var prodCreateResponse = await TestHelpers.CreateKeyValue(client, key, prodValue, label: prodLabel);
            prodCreateResponse.EnsureSuccessStatusCode();

            // Act - Get the key-value by path with label
            var keyValue = await TestHelpers.GetKeyValue(client, key, prodLabel);

            // Assert
            Assert.NotNull(keyValue);
            Assert.Equal(key, keyValue.Key);
            Assert.Equal(prodValue, keyValue.Value);
            Assert.Equal(prodLabel, keyValue.Label);
        }

        [Fact]
        public async Task GetKeyValue_ByQuery_ReturnsCorrectKeyValue()
        {
            // Arrange - Create a key-value
            var client = _testServer.Client;
            var key = "query-test-key";
            var value = "query-test-value";

            // Create the key-value
            var createResponse = await TestHelpers.CreateKeyValue(client, key, value);
            createResponse.EnsureSuccessStatusCode();

            // Act - Get the key-value by query
            var keyValues = await TestHelpers.QueryKeyValues(client, key: key);

            // Assert
            Assert.NotNull(keyValues);
            Assert.NotNull(keyValues.Items);
            Assert.NotEmpty(keyValues.Items);

            var item = keyValues.Items.FirstOrDefault(kv => kv.Key == key);
            Assert.NotNull(item);
            Assert.Equal(value, item.Value);
        }

        [Fact]
        public async Task GetKeyValue_ByQueryWithLabel_ReturnsCorrectKeyValue()
        {
            // Arrange - Create key-values with different labels
            var client = _testServer.Client;
            var key = "query-label-key";
            var devValue = "query-dev-value";
            var prodValue = "query-prod-value";
            var devLabel = "dev";
            var prodLabel = "prod";

            // Create dev key-value
            var devCreateResponse = await TestHelpers.CreateKeyValue(client, key, devValue, label: devLabel);
            devCreateResponse.EnsureSuccessStatusCode();

            // Create prod key-value
            var prodCreateResponse = await TestHelpers.CreateKeyValue(client, key, prodValue, label: prodLabel);
            prodCreateResponse.EnsureSuccessStatusCode();

            // Act - Get the key-value by query with label
            var keyValues = await TestHelpers.QueryKeyValues(client, key: key, label: prodLabel);

            // Assert
            Assert.NotNull(keyValues);
            Assert.NotNull(keyValues.Items);
            Assert.NotEmpty(keyValues.Items);

            var item = keyValues.Items.FirstOrDefault(kv => kv.Key == key && kv.Label == prodLabel);
            Assert.NotNull(item);
            Assert.Equal(prodValue, item.Value);
            Assert.Equal(prodLabel, item.Label);
        }

        [Fact]
        public async Task GetKeyValue_NonExistentKey_ReturnsNotFound()
        {
            // Arrange
            var client = _testServer.Client;
            var nonExistentKey = "non-existent-key";

            // Act - Get a non-existent key-value
            var keyValue = await TestHelpers.GetKeyValue(client, nonExistentKey);

            // Assert
            Assert.Null(keyValue);
        }

        [Fact]
        public async Task GetKeyValue_NonExistentLabel_ReturnsNotFound()
        {
            // Arrange - Create a key-value without a label
            var client = _testServer.Client;
            var key = "no-label-key";
            var value = "no-label-value";

            // Create the key-value
            var createResponse = await TestHelpers.CreateKeyValue(client, key, value);
            createResponse.EnsureSuccessStatusCode();

            // Act - Try to get the key-value with a non-existent label
            var keyValue = await TestHelpers.GetKeyValue(client, key, "non-existent-label");

            // Assert
            Assert.Null(keyValue);
        }

        [Fact]
        public async Task GetKeyValues_NoMatchingItems_ReturnsEmptyList()
        {
            var client = _testServer.Client;

            var createResponse1 = await TestHelpers.CreateKeyValue(client, "existing-key1", "value1");
            createResponse1.EnsureSuccessStatusCode();

            var createResponse2 = await TestHelpers.CreateKeyValue(client, "existing-key2", "value2", label: "dev");
            createResponse2.EnsureSuccessStatusCode();

            var keyValues = await TestHelpers.QueryKeyValues(client, key: "non-matching-key-pattern*", label: "non-existing-label");

            Assert.NotNull(keyValues);
            Assert.NotNull(keyValues.Items);
            Assert.Empty(keyValues.Items); // Verify that items list is empty
        }
    }
}
