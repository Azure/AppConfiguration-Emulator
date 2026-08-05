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

        [Fact]
        public async Task GetSnapshots_NamePrefixFilter_ReturnsMatchingSnapshots()
        {
            var client = _testServer.Client;
            string testId = Guid.NewGuid().ToString("N");
            string key = $"snapshot-filter-key-{testId}";
            string namePrefix = $"snapshot-filter-{testId}-";
            string firstMatchingName = $"{namePrefix}one";
            string secondMatchingName = $"{namePrefix}two";
            string nonMatchingName = $"other-snapshot-{testId}";

            await TestHelpers.CreateKeyValue(client, key, "value");

            var body = new
            {
                composition_type = "key",
                filters = new[]
                {
                    new { key, label = (string)null }
                }
            };
            string json = JsonSerializer.Serialize(body);

            foreach (string snapshotName in new[] { firstMatchingName, secondMatchingName, nonMatchingName })
            {
                var content = new StringContent(json, Encoding.UTF8, "application/vnd.microsoft.appconfig.snapshot+json");
                var response = await client.PutAsync($"/snapshots/{snapshotName}?api-version={ApiVersion}", content);
                Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
            }

            var listResponse = await client.GetAsync($"/snapshots?name={namePrefix}*&api-version={ApiVersion}");
            listResponse.EnsureSuccessStatusCode();
            string responseBody = await listResponse.Content.ReadAsStringAsync();

            Assert.Contains(firstMatchingName, responseBody);
            Assert.Contains(secondMatchingName, responseBody);
            Assert.DoesNotContain(nonMatchingName, responseBody);
        }

        [Theory]
        [InlineData("snap*name")]
        [InlineData("snap,name")]
        public async Task Snapshot_WithReservedCharactersInName_IsRetrievable(string snapshotName)
        {
            var client = _testServer.Client;

            var key = "snap-reserved-key";
            await TestHelpers.CreateKeyValue(client, key, "v1");

            var body = new
            {
                composition_type = "key",
                filters = new[]
                {
                    new { key = key, label = (string)null }
                }
            };
            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/vnd.microsoft.appconfig.snapshot+json");

            // Create - the re-query must find the just-created snapshot and return a Ready body
            var putResponse = await client.PutAsync($"/snapshots/{snapshotName}?api-version={ApiVersion}", content);
            Assert.Equal(System.Net.HttpStatusCode.Created, putResponse.StatusCode);
            var putBody = await putResponse.Content.ReadAsStringAsync();
            Assert.Contains("\"status\":\"ready\"", putBody);

            var getResponse = await client.GetAsync($"/snapshots/{snapshotName}?api-version={ApiVersion}");
            Assert.Equal(System.Net.HttpStatusCode.OK, getResponse.StatusCode);
            Assert.Contains("\"status\":\"ready\"", await getResponse.Content.ReadAsStringAsync());

            var kvPage = await client.GetAsync($"/kv?snapshot={snapshotName}&api-version={ApiVersion}");
            kvPage.EnsureSuccessStatusCode();
            Assert.Contains(key, await kvPage.Content.ReadAsStringAsync());

            var opResponse = await client.GetAsync($"/operations?snapshot={snapshotName}&api-version={ApiVersion}");
            Assert.Equal(System.Net.HttpStatusCode.OK, opResponse.StatusCode);
        }
    }
}
