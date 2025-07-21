namespace Azure.AppConfiguration.Emulator.Host.Tests
{
    public interface ITestServer
    {
        public HttpClient Client { get; }
    }
}
