using Xunit;
using System.Text;
using System.Text.Json;

namespace Azure.AppConfiguration.Emulator.Host.Tests
{
    [Collection("TestServerCollection")]
    public class SnapshotTests
    {
        private readonly ITestServer _testServer;
        private const string ApiVersion = "2024-09-01";
        private const string ApiVersionV26_04 = "2026-04-01";

        public SnapshotTests(TestServerFixture fixture)
        {
            _testServer = fixture.TestServer;
        }

        [Fact]
        public async Task CreateSnapshot_ReturnsReady_AndContent()
        {
            var client = _testServer.Client;

            var key1 = "snap-key-1";
            var key2 = "snap-key-2";
            await TestHelpers.CreateKeyValue(client, key1, "v1", label: "dev");
            await TestHelpers.CreateKeyValue(client, key2, "v2", label: "prod");

            var snapshotName = "snapshot-host-test";
            var snapshotBody = new
            {
                composition_type = "key",
                filters = new[]
                {
                    new { key = key1, label = "dev" },
                    new { key = key2, label = "prod" }
                }
            };

            var json = JsonSerializer.Serialize(snapshotBody);
            var content = new StringContent(json, Encoding.UTF8, "application/vnd.microsoft.appconfig.snapshot+json");
            var putResponse = await client.PutAsync($"/snapshots/{snapshotName}?api-version={ApiVersion}", content);

            Assert.Equal(System.Net.HttpStatusCode.Created, putResponse.StatusCode);

            var getResponse = await client.GetAsync($"/snapshots/{snapshotName}?api-version={ApiVersion}");
            var getContent = await getResponse.Content.ReadAsStringAsync();
            Assert.Contains("\"status\":\"ready\"", getContent);

            var kvPage = await client.GetAsync($"/kv?snapshot={snapshotName}&api-version={ApiVersion}");
            kvPage.EnsureSuccessStatusCode();
            var kvJson = await kvPage.Content.ReadAsStringAsync();
            Assert.Contains(key1, kvJson);
            Assert.Contains(key2, kvJson);
        }

        [Fact]
        public async Task CreateSnapshot_ExistingName_ReturnsConflict()
        {
            var client = _testServer.Client;

            var snapshotName = "snapshot-conflict";
            var body = new
            {
                composition_type = "key",
                filters = new[]
                {
                    new { key = "conflict-key", label = (string)null }
                }
            };

            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/vnd.microsoft.appconfig.snapshot+json");
            var first = await client.PutAsync($"/snapshots/{snapshotName}?api-version={ApiVersion}", content);
            Assert.Equal(System.Net.HttpStatusCode.Created, first.StatusCode);

            // Second PUT with same name should return 409 problem+json
            var second = await client.PutAsync($"/snapshots/{snapshotName}?api-version={ApiVersion}", content);
            Assert.Equal(System.Net.HttpStatusCode.Conflict, second.StatusCode);
            Assert.Equal("application/problem+json", second.Content.Headers.ContentType?.MediaType);

            var err = await second.Content.ReadAsStringAsync();
            Assert.Contains("already-exists", err);
        }

        [Fact]
        public async Task CreateSnapshot_WithDescription_ReturnsDescription()
        {
            var client = _testServer.Client;

            var key = "snap-desc-key";
            await TestHelpers.CreateKeyValue(client, key, "v1", label: "dev");

            var snapshotName = "snapshot-with-description";
            var description = "This is a test description for the snapshot";
            var snapshotBody = new
            {
                description,
                composition_type = "key",
                filters = new[]
                {
                    new { key, label = "dev" }
                }
            };

            var json = JsonSerializer.Serialize(snapshotBody);
            var content = new StringContent(json, Encoding.UTF8, "application/vnd.microsoft.appconfig.snapshot+json");

            // Create using the 2026-04-01 API
            var putResponse = await client.PutAsync($"/snapshots/{snapshotName}?api-version={ApiVersionV26_04}", content);
            Assert.Equal(System.Net.HttpStatusCode.Created, putResponse.StatusCode);

            // Description round-trips on the 2026-04-01 API
            var getResponse = await client.GetAsync($"/snapshots/{snapshotName}?api-version={ApiVersionV26_04}");
            getResponse.EnsureSuccessStatusCode();
            var getContent = await getResponse.Content.ReadAsStringAsync();
            Assert.Contains($"\"description\":\"{description}\"", getContent);

            // Description is not emitted on the older API version
            var oldResponse = await client.GetAsync($"/snapshots/{snapshotName}?api-version={ApiVersion}");
            oldResponse.EnsureSuccessStatusCode();
            var oldContent = await oldResponse.Content.ReadAsStringAsync();
            Assert.DoesNotContain("\"description\"", oldContent);
        }
    }
}
