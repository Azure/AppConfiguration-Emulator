using Xunit;

namespace Azure.AppConfiguration.Emulator.Host.Tests
{
    public class TestServerFixture : IDisposable
    {
        public TestServer TestServer { get; private set; }

        public TestServerFixture()
        {
            TestServer = new TestServer();
        }

        public void Dispose()
        {
            TestServer?.Dispose();
        }
    }

    [CollectionDefinition("TestServerCollection")]
    public class TestServerCollection : ICollectionFixture<TestServerFixture>
    {
        // This class has no code, and is never created. Its purpose is just
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}
