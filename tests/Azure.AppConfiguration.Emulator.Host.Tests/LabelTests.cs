using Xunit;

namespace Azure.AppConfiguration.Emulator.Host.Tests
{
    [Collection("TestServerCollection")]
    public class LabelTests
    {
        private readonly ITestServer _testServer;

        public LabelTests(TestServerFixture fixture)
        {
            _testServer = fixture.TestServer;
        }

        [Fact]
        public async Task GetLabels_ReturnsDistinctLabels()
        {
            // Arrange - Create test key-values with different labels
            var client = _testServer.Client;
            var response1 = await TestHelpers.CreateKeyValue(client, "test-key1", "value1", label: "dev");
            response1.EnsureSuccessStatusCode();

            var response2 = await TestHelpers.CreateKeyValue(client, "test-key2", "value2", label: "prod");
            response2.EnsureSuccessStatusCode();

            var response3 = await TestHelpers.CreateKeyValue(client, "test-key3", "value3", label: "staging");
            response3.EnsureSuccessStatusCode();

            var response4 = await TestHelpers.CreateKeyValue(client, "test-key4", "value4", label: "dev");
            response4.EnsureSuccessStatusCode();

            // Act - Get all labels
            var result = await TestHelpers.GetLabels(client);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Items);
            Assert.NotEmpty(result.Items);

            Assert.Contains(result.Items, l => l.Name == "dev");
            Assert.Contains(result.Items, l => l.Name == "prod");
            Assert.Contains(result.Items, l => l.Name == "staging");
        }

        [Fact]
        public async Task GetLabels_WithName_ReturnsExactLabelMatch()
        {
            // Arrange - Create test key-values with different labels
            var client = _testServer.Client;
            var response1 = await TestHelpers.CreateKeyValue(client, "test-key1", "value1", label: "dev");
            response1.EnsureSuccessStatusCode();

            var response2 = await TestHelpers.CreateKeyValue(client, "test-key2", "value2", label: "prod");
            response2.EnsureSuccessStatusCode();

            var response3 = await TestHelpers.CreateKeyValue(client, "test-key3", "value3", label: "staging");
            response3.EnsureSuccessStatusCode();

            // Act - Get labels with exact name match
            var result = await TestHelpers.GetLabels(client, nameFilter: "prod");

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Items);
            Assert.Single(result.Items);
            Assert.Equal("prod", result.Items[0].Name);
        }

        [Fact]
        public async Task GetLabels_WithWildcardNameFilter_ReturnsMatchingLabels()
        {
            // Arrange - Create test key-values with different labels
            var client = _testServer.Client;
            var response1 = await TestHelpers.CreateKeyValue(client, "test-key1", "value1", label: "dev-1");
            response1.EnsureSuccessStatusCode();

            var response2 = await TestHelpers.CreateKeyValue(client, "test-key2", "value2", label: "dev-2");
            response2.EnsureSuccessStatusCode();

            var response3 = await TestHelpers.CreateKeyValue(client, "test-key3", "value3", label: "prod");
            response3.EnsureSuccessStatusCode();

            // Act - Get labels with wildcard name filter
            var result = await TestHelpers.GetLabels(client, nameFilter: "dev*");

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Items);
            Assert.NotEmpty(result.Items);

            Assert.All(result.Items, label => Assert.StartsWith("dev", label.Name));

            Assert.Contains(result.Items, l => l.Name == "dev-1");
            Assert.Contains(result.Items, l => l.Name == "dev-2");

            Assert.DoesNotContain(result.Items, l => l.Name == "prod");
        }

        [Fact]
        public async Task GetLabels_WithMultipleNameFilter_ReturnsMatchingLabels()
        {
            // Arrange - Create test key-values with different labels
            var client = _testServer.Client;
            var response1 = await TestHelpers.CreateKeyValue(client, "test-key1", "value1", label: "dev");
            response1.EnsureSuccessStatusCode();

            var response2 = await TestHelpers.CreateKeyValue(client, "test-key2", "value2", label: "prod");
            response2.EnsureSuccessStatusCode();

            var response3 = await TestHelpers.CreateKeyValue(client, "test-key3", "value3", label: "staging");
            response3.EnsureSuccessStatusCode();

            // Act - Get labels with multiple name filter (comma-separated)
            var result = await TestHelpers.GetLabels(client, nameFilter: "dev,staging");

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Items);
            Assert.NotEmpty(result.Items);

            Assert.Equal(2, result.Items.Count);

            Assert.Contains(result.Items, l => l.Name == "dev");
            Assert.Contains(result.Items, l => l.Name == "staging");

            Assert.DoesNotContain(result.Items, l => l.Name == "prod");
        }

        [Fact]
        public async Task GetLabels_WithAsteriskNameFilter_ReturnsAllLabels()
        {
            // Arrange - Create test key-value with a label
            var client = _testServer.Client;
            var response = await TestHelpers.CreateKeyValue(client, "test-key", "test-value", label: "test-label");
            response.EnsureSuccessStatusCode();

            // Act - Get labels with asterisk wildcard
            var result = await TestHelpers.GetLabels(client, nameFilter: "*");

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Items);
            Assert.NotEmpty(result.Items);

            Assert.Contains(result.Items, l => l.Name == "test-label");
        }
    }
}
