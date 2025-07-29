using System.Text;
using System.Text.Json;

namespace Azure.AppConfiguration.Emulator.Host.Tests
{
    /// <summary>
    /// Helper methods for common test operations
    /// </summary>
    public static class TestHelpers
    {
        public static async Task<HttpResponseMessage> CreateKeyValue(
            HttpClient client,
            string key,
            string value,
            string label = null,
            Dictionary<string, string> tags = null,
            string contentType = null)
        {
            var keyValueObject = new
            {
                value,
                content_type = contentType,
                tags
            };

            var keyValueJson = JsonSerializer.Serialize(keyValueObject);
            var keyValueContent = new StringContent(
                keyValueJson,
                Encoding.UTF8,
                "application/vnd.microsoft.appconfig.kv+json");

            string url = $"/kv/{key}";
            if (!string.IsNullOrEmpty(label))
            {
                url += $"?label={label}";
            }

            return await client.PutAsync(url, keyValueContent);
        }

        public static async Task<KeysResponse> GetKeys(HttpClient client, string nameFilter = null)
        {
            string url = "/keys";
            if (!string.IsNullOrEmpty(nameFilter))
            {
                url += $"?name={nameFilter}";
            }

            var response = await client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<KeysResponse>(content, options);
        }

        public static async Task<LabelsResponse> GetLabels(HttpClient client, string nameFilter = null)
        {
            string url = "/labels";
            if (!string.IsNullOrEmpty(nameFilter))
            {
                url += $"?name={nameFilter}";
            }

            var response = await client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<LabelsResponse>(content, options);
        }

        public static async Task<KeyValue> GetKeyValue(HttpClient client, string key, string label = null)
        {
            string url = $"/kv/{key}";
            if (!string.IsNullOrEmpty(label))
            {
                url += $"?label={label}";
            }

            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<KeyValue>(content, options);
        }

        public static async Task<KeyValuesResponse> QueryKeyValues(HttpClient client, string key = null, string label = null)
        {
            var queryParams = new List<string>();

            if (!string.IsNullOrEmpty(key))
            {
                queryParams.Add($"key={key}");
            }

            if (!string.IsNullOrEmpty(label))
            {
                queryParams.Add($"label={label}");
            }

            string url = "/kv";
            if (queryParams.Any())
            {
                url += $"?{string.Join("&", queryParams)}";
            }

            var response = await client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<KeyValuesResponse>(content, options);
        }

        public static async Task<HttpResponseMessage> DeleteKeyValue(HttpClient client, string key, string label = null)
        {
            string url = $"/kv/{key}";
            if (!string.IsNullOrEmpty(label))
            {
                url += $"?label={label}";
            }

            return await client.DeleteAsync(url);
        }

        public static async Task<KeyValuesResponse> GetRevisions(HttpClient client, string key = null, string label = null)
        {
            var queryParams = new List<string>();

            if (!string.IsNullOrEmpty(key))
            {
                queryParams.Add($"key={key}");
            }

            if (!string.IsNullOrEmpty(label))
            {
                queryParams.Add($"label={label}");
            }

            string url = "/revisions";
            if (queryParams.Any())
            {
                url += $"?{string.Join("&", queryParams)}";
            }

            var response = await client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<KeyValuesResponse>(content, options);
        }
    }

    public class KeysResponse
    {
        public List<Key> Items { get; set; }

        public class Key
        {
            public string Name { get; set; }
        }
    }

    public class LabelsResponse
    {
        public List<Label> Items { get; set; }

        public class Label
        {
            public string Name { get; set; }
        }
    }

    public class KeyValue
    {
        public string Key { get; set; }
        public string Label { get; set; }
        public string Value { get; set; }
        public string ContentType { get; set; }
        public string ETag { get; set; }
        public Dictionary<string, string> Tags { get; set; }
    }

    public class KeyValuesResponse
    {
        public List<KeyValue> Items { get; set; }
    }
}
