using System.Collections.Generic;

namespace Azure.AppConfiguration.Emulator.Diagnostics
{
    public class HttpLoggingOptions
    {
        /// <summary>
        /// Maximium length (char count) of log attribute value.
        /// </summary>
        public int MaxLogValueLength { get; set; } = 50;

        /// <summary>
        /// Maximium length (char count) of UserAgent value.
        /// </summary>
        public int MaxUserAgentLength { get; set; } = 300;

        /// <summary>
        /// Request headers that need to be logged in HTTP log.
        /// An example to config it in appsettings.json:
        /// "RequestHeaders": [{"LogAttributeName" : "UserAgent", "HeaderName": "User-Agent", "MaxSize": 1000}]
        /// </summary>
        public IEnumerable<LogHeaderInfo> RequestHeaders { get; set; }

        /// <summary>
        /// Response headers that need to be logged in HTTP log.
        /// An example to config it in appsettings.json:
        /// "ResponseHeaders": [{"LogAttributeName" : "RetryAfter", "HeaderName": "retry-after-ms"}]
        /// </summary>
        public IEnumerable<LogHeaderInfo> ResponseHeaders { get; set; }
    }
}
