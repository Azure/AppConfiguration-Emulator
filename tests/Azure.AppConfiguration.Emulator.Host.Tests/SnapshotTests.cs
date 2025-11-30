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
    }
}
