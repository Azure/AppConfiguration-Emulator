// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.ConfigurationSnapshots
{
    public interface ISnapshotContentsStorage
    {
        Task<Snapshot> Provision(Snapshot snapshot, CancellationToken cancellationToken);
    }
}
