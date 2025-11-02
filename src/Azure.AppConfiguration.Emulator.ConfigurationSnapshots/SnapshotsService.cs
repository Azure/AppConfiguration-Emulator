// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.ConfigurationSnapshots
{
    /// <summary>
    /// Background service that periodically inspects snapshot metadata and provisions content for snapshots in Provisioning state.
    /// </summary>
    public class SnapshotsService : BackgroundService
    {
        private readonly ISnapshotsStorage _storage;
        private readonly ISnapshotContentsStorage _contentsStorage;
        private readonly IKeyValueProvider _kvProvider;
        private readonly ILogger<SnapshotsService> _logger;
        private static readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(5);

        public SnapshotsService(
            ISnapshotsStorage storage,
            ISnapshotContentsStorage contentsStorage,
            IKeyValueProvider kvProvider,
            ILogger<SnapshotsService> logger)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _contentsStorage = contentsStorage ?? throw new ArgumentNullException(nameof(contentsStorage));
            _kvProvider = kvProvider ?? throw new ArgumentNullException(nameof(kvProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SnapshotsService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessSnapshotsAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SnapshotsService iteration failed.");
                }

                try
                {
                    await Task.Delay(_pollInterval, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }

            _logger.LogInformation("SnapshotsService stopped.");
        }

        private async Task ProcessSnapshotsAsync(CancellationToken cancellationToken)
        {
            int provisioned = 0;

            Console.WriteLine("Checking for snapshots to provision...");

            await foreach (Snapshot snapshot in _storage.QuerySnapshots().WithCancellation(cancellationToken))
            {
                if (snapshot == null || snapshot.Status != SnapshotStatus.Provisioning)
                {
                    continue;
                }

                IEnumerable<KeyValue> items = await CollectItemsAsync(snapshot, cancellationToken);

                string filePath = BuildSnapshotContentFileName(snapshot.Id);

                MediaInfo media = await _contentsStorage.CreateContent(filePath, items, cancellationToken);

                snapshot.Media = media;
                snapshot.ItemCount = items.Count();
                snapshot.Size = media.Size;
                snapshot.Status = SnapshotStatus.Ready;
                snapshot.StatusCode = 200;
                snapshot.LastModified = DateTimeOffset.UtcNow;

                await _storage.UpdateSnapshot(snapshot, cancellationToken);

                if (snapshot.Status == SnapshotStatus.Ready)
                {
                    provisioned++;
                    _logger.LogInformation("Provisioned snapshot {SnapshotId}.", snapshot.Id);
                }
            }

            if (provisioned > 0)
            {
                _logger.LogInformation("Provisioned {Provisioned} snapshot(s) this cycle.", provisioned);
            }
        }

        private async Task<IEnumerable<KeyValue>> CollectItemsAsync(Snapshot snapshot, CancellationToken cancellationToken)
        {
            if (snapshot.Filters == null)
            {
                return Enumerable.Empty<KeyValue>();
            }

            var result = new List<KeyValue>();
            foreach (KeyValueFilter f in snapshot.Filters)
            {
                var keyFilter = new StringFilter { EqualsTo = f.Key, IsNull = f.Key == null };
                var labelFilter = new StringFilter { EqualsTo = f.Label, IsNull = f.Label == null };
                string continuation = null;
                do
                {
                    var page = await _kvProvider.QueryKeyValues(new KeyValueSearchOptions
                    {
                        KeyFilter = keyFilter,
                        LabelFilter = labelFilter,
                        ContinuationToken = continuation,
                        Tags = f.Tags
                    }, cancellationToken);

                    result.AddRange(page);
                    continuation = page.ContinuationToken;
                }
                while (!string.IsNullOrEmpty(continuation));
            }

            return result.GroupBy(k => (k.Key, k.Label)).Select(g => g.First());
        }

        private string BuildSnapshotContentFileName(string snapshotId)
        {
            return snapshotId + ".ndjson";
        }
    }
}
