// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.ConfigurationSnapshots
{
    /// <summary>
    /// Implements snapshot lifecycle operations (provision content, update status).
    /// </summary>
    public sealed class SnapshotsManager : ISnapshotsManager
    {
        private readonly ISnapshotsStorage _storage;
        private readonly IKeyValueProvider _keyValueProvider;
        private readonly ILogger<SnapshotsManager> _logger;

        public SnapshotsManager(
            ISnapshotsStorage storage,
            IKeyValueProvider keyValueProvider,
            ILogger<SnapshotsManager> logger)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _keyValueProvider = keyValueProvider ?? throw new ArgumentNullException(nameof(keyValueProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Snapshot> Provision(Snapshot snapshot, CancellationToken cancellationToken)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            if (snapshot.Status != SnapshotStatus.Provisioning)
            {
                return snapshot; // No action required
            }

            await ProvisionSnapshotAsync(snapshot, cancellationToken);
            return snapshot;
        }

        private async Task ProvisionSnapshotAsync(Snapshot snapshot, CancellationToken cancellationToken)
        {
            try
            {
                var items = await GetSnapshotContent(snapshot, cancellationToken);
                await _storage.SaveSnapshotContent(snapshot, items, cancellationToken);

                snapshot.Status = SnapshotStatus.Ready;
                snapshot.StatusCode = 200;
                snapshot.LastModified = DateTimeOffset.UtcNow;
                await _storage.UpdateSnapshot(snapshot, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to provision snapshot {SnapshotId}.", snapshot.Id);

                try
                {
                    snapshot.Status = SnapshotStatus.Failed;
                    snapshot.StatusCode = 500;
                    snapshot.LastModified = DateTimeOffset.UtcNow;
                    await _storage.UpdateSnapshot(snapshot, cancellationToken);
                }
                catch (Exception inner)
                {
                    _logger.LogError(inner, "Failed to mark snapshot {SnapshotId} as failed.", snapshot.Id);
                }
            }
        }

        private async Task<IEnumerable<KeyValue>> GetSnapshotContent(Snapshot snapshot, CancellationToken cancellationToken)
        {
            List<KeyValue> result = new();

            if (snapshot.Filters == null)
            {
                return result; // empty snapshot
            }

            foreach (var f in snapshot.Filters)
            {
                var keyFilter = new StringFilter
                {
                    EqualsTo = f.Key,
                    IsNull = f.Key == null
                };
                var labelFilter = new StringFilter
                {
                    EqualsTo = f.Label,
                    IsNull = f.Label == null
                };

                string continuation = null;
                do
                {
                    var page = await _keyValueProvider.QueryKeyValues(
                        new KeyValueSearchOptions
                        {
                            KeyFilter = keyFilter,
                            LabelFilter = labelFilter,
                            ContinuationToken = continuation
                        },
                        cancellationToken);

                    result.AddRange(page);
                    continuation = page.ContinuationToken;
                }
                while (!string.IsNullOrEmpty(continuation));
            }

            return result
                .GroupBy(k => (k.Key, k.Label))
                .Select(g => g.First());
        }
    }
}
