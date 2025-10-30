// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.ConfigurationSnapshots
{
    /// <summary>
    /// Snapshot lifecycle operations.
    /// </summary>
    public interface ISnapshotsManager
    {
        /// <summary>
        /// Provision the specified snapshot if it is in provisioning state and return the updated snapshot.
        /// </summary>
        Task<Snapshot> Provision(Snapshot snapshot, CancellationToken cancellationToken);
    }
}
