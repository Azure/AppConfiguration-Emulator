using System;

namespace Azure.AppConfiguration.Emulator.ConfigurationSettings
{
    public class KeySearchOptions
    {
        public string Key { get; set; }

        public string ContinuationToken { get; set; }

        public Range Range { get; set; }

        public DateTimeOffset? TimeGate { get; set; }
    }
}
