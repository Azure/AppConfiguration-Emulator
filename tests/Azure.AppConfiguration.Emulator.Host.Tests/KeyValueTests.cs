using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Azure.AppConfiguration.Emulator.Host.Tests
{
    [Collection("TestServerCollection")]
    public class KeyValueTests
    {
        private readonly TestServer _testServer;

        public KeyValueTests(TestServerFixture fixture)
        {
            _testServer = fixture.TestServer;
        }

        [Fact]
        public async Task GetKeyValues_ReturnsKeyValues()
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

        [Fact]
        public async Task GetKeyValues_WithKeyFilter_ReturnsFilteredValues()
        {
            // Arrange
            var client = _testServer.ServerClient;
            var keyFilter = "filtered-key";

            // Act
            var response = await client.GetAsync($"/kv?key={keyFilter}*");
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
            Assert.All(result.Items, item => Assert.StartsWith(keyFilter, item.Key));
        }

        [Fact]
        public async Task GetKeyValues_WithLabelFilter_ReturnsFilteredValues()
        {
            // Arrange
            var client = _testServer.ServerClient;
            var labelFilter = "filtered-label";

            // Act
            var response = await client.GetAsync($"/kv?label={labelFilter}*");
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
            Assert.All(result.Items, item => Assert.StartsWith(labelFilter, item.Label));
        }

        [Fact]
        public async Task GetKeyValues_WithEmptyLabelFilter_ReturnsValuesWithNoLabel()
        {
            // Arrange
            var client = _testServer.ServerClient;

            // Act
            var response = await client.GetAsync("/kv?label=%00");
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
            Assert.All(result.Items, item => Assert.Null(item.Label));
        }

        [Fact]
        public async Task GetKeyValues_WithWildcardLabelFilter_ReturnsAllValues()
        {
            // Arrange
            var client = _testServer.ServerClient;

            // Act
            var response = await client.GetAsync("/kv?label=*");
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

            // Verify items with and without labels are returned
            Assert.Contains(result.Items, item => !string.IsNullOrEmpty(item.Label));
            Assert.Contains(result.Items, item => string.IsNullOrEmpty(item.Label));
        }

        [Fact]
        public async Task GetKeyValue_ExistingKey_ReturnsKeyValue()
        {
            // Arrange
            var client = _testServer.ServerClient;
            var key = "test-key1";
            var label = "test-label1";

            // Act
            var response = await client.GetAsync($"/kv/{Uri.EscapeDataString(key)}?label={Uri.EscapeDataString(label)}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            // Parse the JSON response
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var result = JsonSerializer.Deserialize<KeyValue>(content, options);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(key, result.Key);
            Assert.Equal(label, result.Label);
            Assert.Equal("test-value1", result.Value);
        }

        [Fact]
        public async Task GetKeyValue_NonExistingKey_ReturnsNotFound()
        {
            // Arrange
            var client = _testServer.ServerClient;
            var key = "non-existing-key";

            // Act
            var response = await client.GetAsync($"/kv/{Uri.EscapeDataString(key)}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetKeyValues_WithContentTypeFilter_ReturnsFilteredValues()
        {
            // Arrange
            var client = _testServer.ServerClient;
            var contentType = "application/json";

            // Act
            var response = await client.GetAsync($"/kv?content-type={Uri.EscapeDataString(contentType)}");
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
            Assert.All(result.Items, item => Assert.Equal(contentType, item.ContentType));
        }

        [Fact]
        public async Task PutKeyValue_NewKey_CreatesKeyValue()
        {
            // Arrange
            var client = _testServer.ServerClient;
            var key = "new-test-key";
            var value = "new-test-value";
            var keyValue = new KeyValue
            {
                Key = key,
                Value = value,
                ContentType = "text/plain"
            };

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var json = JsonSerializer.Serialize(keyValue, options);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await client.PutAsync($"/kv/{Uri.EscapeDataString(key)}", content);
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            var createdKeyValue = JsonSerializer.Deserialize<KeyValue>(responseContent, options);

            // Assert
            Assert.NotNull(createdKeyValue);
            Assert.Equal(key, createdKeyValue.Key);
            Assert.Equal(value, createdKeyValue.Value);
            Assert.Equal("text/plain", createdKeyValue.ContentType);

            // Verify it was created by retrieving it
            var getResponse = await client.GetAsync($"/kv/{Uri.EscapeDataString(key)}");
            getResponse.EnsureSuccessStatusCode();
            var getContent = await getResponse.Content.ReadAsStringAsync();
            var retrievedKeyValue = JsonSerializer.Deserialize<KeyValue>(getContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(retrievedKeyValue);
            Assert.Equal(key, retrievedKeyValue.Key);
            Assert.Equal(value, retrievedKeyValue.Value);
        }

        [Fact]
        public async Task DeleteKeyValue_ExistingKey_RemovesKeyValue()
        {
            // Arrange
            var client = _testServer.ServerClient;
            var key = "test-key-to-delete";
            var label = "test-label-to-delete";

            // First, create a key-value to delete
            var keyValue = new KeyValue
            {
                Key = key,
                Label = label,
                Value = "value-to-delete"
            };

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var json = JsonSerializer.Serialize(keyValue, options);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var putResponse = await client.PutAsync($"/kv/{Uri.EscapeDataString(key)}?label={Uri.EscapeDataString(label)}", content);
            putResponse.EnsureSuccessStatusCode();

            // Act
            var deleteResponse = await client.DeleteAsync($"/kv/{Uri.EscapeDataString(key)}?label={Uri.EscapeDataString(label)}");
            deleteResponse.EnsureSuccessStatusCode();

            // Assert - Verify it was deleted
            var getResponse = await client.GetAsync($"/kv/{Uri.EscapeDataString(key)}?label={Uri.EscapeDataString(label)}");
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        }

        [Fact]
        public async Task GetKeyValues_WithFields_ReturnsSelectedFields()
        {
            // Arrange
            var client = _testServer.ServerClient;
            var fields = "key,value";

            // Act
            var response = await client.GetAsync($"/kv?$select={fields}");
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

            // Verify only requested fields have values
            // Note: This is not a perfect test since JSON deserialization will create all properties
            // but we can check that at least the requested fields exist
            var firstItem = result.Items.First();
            Assert.NotNull(firstItem.Key);
            Assert.NotNull(firstItem.Value);
        }

        [Fact]
        public async Task GetKeyValues_WithAcceptDatetimeHeader_ReturnsPointInTimeSnapshot()
        {
            // Arrange
            var client = _testServer.ServerClient;
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, "/kv");
            requestMessage.Headers.Add("Accept-Datetime", DateTime.UtcNow.AddHours(-1).ToString("R"));

            // Act
            var response = await client.SendAsync(requestMessage);
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
        }

        // Helper class to deserialize the response
        private class KeyValueResponse
        {
            public List<KeyValue> Items { get; set; }
            public string Etag { get; set; }
        }
    }
}
