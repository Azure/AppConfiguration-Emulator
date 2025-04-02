// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Azure.AppConfiguration.Emulator.ConfigurationSnapshots
{
    public enum SnapshotStatus
    {
        None,
        Provisioning,
        Ready,
        Archived,
        Failed
    }
}
