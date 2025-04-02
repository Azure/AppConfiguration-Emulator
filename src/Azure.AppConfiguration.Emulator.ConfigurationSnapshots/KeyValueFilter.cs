// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;

namespace Azure.AppConfiguration.Emulator.ConfigurationSnapshots
{
    public class KeyValueFilter
    {
        public string Key { get; set; }

        public string Label { get; set; }

        public IEnumerable<KeyValuePair<string, string>> Tags { get; set; }
    }
}
