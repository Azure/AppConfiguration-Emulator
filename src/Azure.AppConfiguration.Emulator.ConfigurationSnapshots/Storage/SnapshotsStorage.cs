// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.ConfigurationSnapshots
{
    public class SnapshotsStorage : ISnapshotsStorage
    {
        private readonly SnapshotProviderOptions _options;

        public SnapshotsStorage(IOptions<SnapshotProviderOptions> options)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public IAsyncEnumerable<Snapshot> QuerySnapshots()
        {
            //
            // TODO: Add implementation
            //

            return AsyncEnumerable.Empty<Snapshot>();
        }

        public Task<Snapshot> GetSnapshot(string snapshotId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(snapshotId))
            {
                throw new ArgumentNullException(nameof(snapshotId));
            }

            //
            // TODO: Add implementation
            //

            return Task.FromResult<Snapshot>(null);
        }

        public Task AddSnapshot(Snapshot snapshot, CancellationToken cancellationToken)
        {
            ValidateSnapshot(snapshot);

            //
            // TODO: Add implementation
            //

            return Task.CompletedTask;
        }

        public Task UpdateSnapshot(Snapshot snapshot, CancellationToken cancellationToken)
        {
            ValidateSnapshot(snapshot);

            if (string.IsNullOrEmpty(snapshot.Etag))
            {
                throw new ArgumentNullException(nameof(snapshot.Etag));
            }

            //
            // TODO: Add implementation
            //

            return Task.CompletedTask;
        }

        public IAsyncEnumerable<KeyValue> ReadSnapshotContent(Snapshot snapshot, long offset)
        {
            ValidateSnapshot(snapshot);

            //
            // TODO: Add implementation
            //

            return AsyncEnumerable.Empty<KeyValue>();
        }

        private static void ValidateSnapshot(Snapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            if (string.IsNullOrEmpty(snapshot.Id))
            {
                throw new ArgumentNullException(nameof(snapshot.Id));
            }
        }
    }
}
