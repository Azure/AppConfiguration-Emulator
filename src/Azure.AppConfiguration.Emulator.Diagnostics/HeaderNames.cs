namespace Azure.AppConfiguration.Emulator.Diagnostics
{
    static class HeaderNames
    {
        public const string RequestId = "x-ms-request-id";
        public const string ClientRequestId = "x-ms-client-request-id";
        public const string CorrelationRequestId = "x-ms-correlation-request-id";
        public const string ReturnClientRequestId = "x-ms-return-client-request-id";
        public const string TenantName = "x-ms-tenant-name";
        public const string SubscriptionId = "x-ms-subscription-id";
        public const string ResourceGroup = "x-ms-resource-group";
        public const string XForwardedFor = "X-Forwarded-For";
        public const string MsUserAgent = "x-ms-useragent";
        public const string UserAgent = "User-Agent";
        public const string SSLCipher = "x-ms-ssl-cipher";
        public const string SSLProtocol = "x-ms-ssl-protocol";
    }
}
