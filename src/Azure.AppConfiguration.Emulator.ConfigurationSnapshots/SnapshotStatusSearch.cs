// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Azure.AppConfiguration.Emulator.ConfigurationSnapshots
{
    [Flags]
    public enum SnapshotStatusSearch
    {
        None = 0,
        Provisioning = 1,
        Ready = 2,
        Archived = 4,
        Failed = 8,
        All = 0xF
    }
}
