// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;

namespace Azure.AppConfiguration.Emulator.ConfigurationSettings
{
    public class KeyValueSearchOptions
    {
        public StringFilter KeyFilter { get; set; }

        public StringFilter LabelFilter { get; set; }

        public IEnumerable<KeyValuePair<string, string>> Tags { get; set; }

        public string ContinuationToken { get; set; }

        public string SnapshotName { get; set; }

        public DateTimeOffset? TimeGate { get; set; }
    }
}
