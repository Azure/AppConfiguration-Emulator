using System.Text.Json;
using Xunit;

namespace Azure.AppConfiguration.Emulator.Host.Tests
{
    [Collection("TestServerCollection")]
    public class LabelTests
    {
        private readonly TestServer _testServer;

        public LabelTests(TestServerFixture fixture)
        {
            _testServer = fixture.TestServer;
        }

        [Fact]
        public async Task GetLabels_ReturnsDistinctLabels()
        {
            // Arrange
            var client = _testServer.ServerClient;

            // Act
            var response = await client.GetAsync("/labels");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            // Parse the JSON response
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var result = JsonSerializer.Deserialize<LabelsResponse>(content, options);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Items);
            Assert.NotEmpty(result.Items);

            // Verify test labels are returned - we expect at least test-label1, dev, and prod
            // from the seed data in TestKeyValueStorage
            var testLabel1 = result.Items.Find(l => l.Name == "test-label1");
            Assert.NotNull(testLabel1);

            var devLabel = result.Items.Find(l => l.Name == "dev");
            Assert.NotNull(devLabel);

            var prodLabel = result.Items.Find(l => l.Name == "prod");
            Assert.NotNull(prodLabel);
        }

        [Fact]
        public async Task GetLabels_WithFilter_ReturnsFilteredLabels()
        {
            // Arrange
            var client = _testServer.ServerClient;

            // Act - request labels with a filter (only get test-label1)
            var response = await client.GetAsync("/labels?name=test-label1");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            // Parse the JSON response
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var result = JsonSerializer.Deserialize<LabelsResponse>(content, options);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Items);
            Assert.NotEmpty(result.Items);

            // Should only have test-label1
            Assert.Single(result.Items);
            Assert.Equal("test-label1", result.Items[0].Name);

            // Should not have other labels
            Assert.DoesNotContain(result.Items, l => l.Name == "dev");
            Assert.DoesNotContain(result.Items, l => l.Name == "prod");
        }

        // Helper class to deserialize the response
        private class LabelsResponse
        {
            public List<Label> Items { get; set; }

            public class Label
            {
                public string Name { get; set; }
            }
        }
    }
}
