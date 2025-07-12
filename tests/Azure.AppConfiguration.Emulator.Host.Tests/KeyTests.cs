using Xunit;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;

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

        [Fact]
        public async Task GetKeys_WithPagination_ReturnsPagedResults()
        {
            // Arrange
            var client = _testServer.ServerClient;
            int pageSize = 2;

            // Act - Get first page
            var response = await client.GetAsync($"/keys?$top={pageSize}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            // Parse the JSON response
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var firstPage = JsonSerializer.Deserialize<KeysResponse>(content, options);

            // Assert first page
            Assert.NotNull(firstPage);
            Assert.NotNull(firstPage.Items);
            Assert.Equal(pageSize, firstPage.Items.Count);

            // Check for continuation token (Skip validation if token not in response, as it depends on server implementation)
            if (response.Headers.Contains("Link"))
            {
                // Extract next link from headers
                var linkHeader = response.Headers.GetValues("Link").FirstOrDefault();
                Assert.NotNull(linkHeader);

                // Parse the URL from the Link header
                var nextLink = linkHeader.Split(';')[0].Trim('<', '>');
                Assert.NotEmpty(nextLink);

                // Act - Get second page using the next link
                var secondPageResponse = await client.GetAsync(nextLink);
                secondPageResponse.EnsureSuccessStatusCode();
                var secondPageContent = await secondPageResponse.Content.ReadAsStringAsync();
                var secondPage = JsonSerializer.Deserialize<KeysResponse>(secondPageContent, options);

                // Assert second page
                Assert.NotNull(secondPage);
                Assert.NotNull(secondPage.Items);
                Assert.NotEmpty(secondPage.Items);

                // Verify first and second page have different items
                var firstPageKeys = firstPage.Items.Select(k => k.Name).ToList();
                var secondPageKeys = secondPage.Items.Select(k => k.Name).ToList();
                Assert.Empty(firstPageKeys.Intersect(secondPageKeys));
            }
        }

        [Fact]
        public async Task GetKey_ExistingKey_ReturnsKey()
        {
            // Arrange
            var client = _testServer.ServerClient;
            var keyName = "test-key1";

            // Act
            var response = await client.GetAsync($"/keys/{Uri.EscapeDataString(keyName)}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            // Parse the JSON response
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var result = JsonSerializer.Deserialize<KeysResponse.Key>(content, options);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(keyName, result.Name);
        }

        [Fact]
        public async Task GetKey_NonExistingKey_ReturnsNotFound()
        {
            // Arrange
            var client = _testServer.ServerClient;
            var keyName = "non-existing-key";

            // Act
            var response = await client.GetAsync($"/keys/{Uri.EscapeDataString(keyName)}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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
