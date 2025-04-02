// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.ConfigurationSnapshots
{
    public interface ISnapshotProvider
    {
        Task<IEnumerable<Snapshot>> Get(SnapshotSearchOptions options, CancellationToken cancellationToken);

        Task Create(Snapshot snapshot, CancellationToken cancellationToken);

        Task Archive(Snapshot snapshot, CancellationToken cancellationToken);

        Task Recover(Snapshot snapshot, CancellationToken cancellationToken);

        Task<IEnumerable<KeyValue>> GetContent(Snapshot snapshot, SnapshotContentSearchOptions options, CancellationToken cancellationToken);
    }
}
