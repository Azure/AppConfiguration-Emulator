using System.Net;
using Xunit;

namespace Azure.AppConfiguration.Emulator.Host.Tests
{
    [Collection("TestServerCollection")]
    public class HealthTests
    {
        private readonly ITestServer _testServer;

        public HealthTests(TestServerFixture fixture)
        {
            _testServer = fixture.TestServer;
        }

        [Fact]
        public async Task GetHealth_ReturnsOk()
        {
            HttpResponseMessage response = await _testServer.Client.GetAsync("/health");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}