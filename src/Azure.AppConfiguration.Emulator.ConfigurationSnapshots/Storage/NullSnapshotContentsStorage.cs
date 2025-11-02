// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.ConfigurationSnapshots
{
    internal sealed class NullSnapshotContentsStorage : ISnapshotContentsStorage
    {
        public Task<Snapshot> Provision(Snapshot snapshot, CancellationToken cancellationToken)
        {
            // No-op provisioning.
            return Task.FromResult(snapshot);
        }

        public async IAsyncEnumerable<KeyValue> Get(
            Snapshot snapshot,
            long offset,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            // Always yields nothing.
            yield break;
        }
    }
}
