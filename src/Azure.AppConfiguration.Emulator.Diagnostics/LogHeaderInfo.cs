namespace Azure.AppConfiguration.Emulator.Diagnostics
{
    public class LogHeaderInfo
    {
        /// <summary>
        /// Http header name.
        /// </summary>
        public string HeaderName { get; set; }

        /// <summary>
        /// The name of header in log.
        /// </summary>
        public string LogAttributeName { get; set; }

        /// <summary>
        /// Maximum length (char count) of the header value.
        /// </summary>
        public int MaxLength { get; set; } = int.MaxValue;
    }
}
