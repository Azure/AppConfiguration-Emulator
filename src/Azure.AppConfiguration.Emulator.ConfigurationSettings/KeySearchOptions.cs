// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Azure.AppConfiguration.Emulator.ConfigurationSettings
{
    public class KeySearchOptions
    {
        public StringFilter KeyFilter { get; set; }

        public string ContinuationToken { get; set; }

        public DateTimeOffset? TimeGate { get; set; }
    }
}
