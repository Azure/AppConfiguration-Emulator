using System.Net;
using Xunit;

namespace Azure.AppConfiguration.Emulator.Host.Tests
{
    [Collection("TestServerCollection")]
    public class SyncTokenTests
    {
        private readonly ITestServer _testServer;

        public SyncTokenTests(TestServerFixture fixture)
        {
            _testServer = fixture.TestServer;
        }

        [Fact]
        public async Task CreateAndGetKeyIncludeSyncTokenHeader()
        {
            var client = _testServer.Client;
            string key = $"sync-token-test-key";

            var createResponse = await TestHelpers.CreateKeyValue(client, key, "value");
            createResponse.EnsureSuccessStatusCode();

            Assert.True(createResponse.Headers.TryGetValues("Sync-Token", out var createValues));
            Assert.Contains("kv=MA==;sn=1", createValues);

            var getResponse = await client.GetAsync($"/kv/{key}");
            getResponse.EnsureSuccessStatusCode();

            Assert.True(getResponse.Headers.TryGetValues("Sync-Token", out var getValues));
            Assert.Contains("kv=MA==;sn=1", getValues);

            var keyValue = await TestHelpers.GetKeyValue(client, key);
            Assert.NotNull(keyValue);
            Assert.Equal(key, keyValue.Key);
            Assert.Equal("value", keyValue.Value);
        }

        [Fact]
        public async Task GetUnexistingKeyIncludesSyncTokenHeader()
        {
            var client = _testServer.Client;
            string key = $"sync-token-unexisting-key";

            var response = await client.GetAsync($"/kv/{key}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.True(response.Headers.TryGetValues("Sync-Token", out var values));
            Assert.Contains("kv=MA==;sn=1", values);
        }
    }
}
