// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.AppConfiguration.Emulator.Search;

namespace Azure.AppConfiguration.Emulator.ConfigurationSnapshots
{
    public class SnapshotSearchOptions
    {
        public StringFilter NameFilter { get; set; }

        public SnapshotStatusSearch Status { get; set; }

        public string ContinuationToken { get; set; }
    }
}
