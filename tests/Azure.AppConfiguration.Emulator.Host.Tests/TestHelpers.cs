using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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
}
