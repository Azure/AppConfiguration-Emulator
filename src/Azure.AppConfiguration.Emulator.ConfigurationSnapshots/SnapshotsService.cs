// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.ConfigurationSnapshots
{
    /// <summary>
    /// Background service that periodically inspects snapshot metadata.
    /// </summary>
    public class SnapshotsService : BackgroundService
    {
        private readonly ISnapshotsStorage _storage;
        private readonly ISnapshotContentsStorage _contents;
        private readonly ILogger<SnapshotsService> _logger;
        private static readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(10);

        public SnapshotsService(
            ISnapshotsStorage storage,
            ISnapshotContentsStorage contents,
            ILogger<SnapshotsService> logger)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            if (contents == null) throw new ArgumentNullException(nameof(contents));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            _storage = storage;
            _contents = contents;
            _logger = logger;
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

            await foreach (var snapshot in _storage.QuerySnapshots().WithCancellation(cancellationToken))
            {
                if (snapshot == null)
                {
                    continue;
                }

                SnapshotStatus before = snapshot.Status;
                Snapshot updated = await _contents.Provision(snapshot, cancellationToken);

                if (before == SnapshotStatus.Provisioning)
                {
                    await _storage.UpdateSnapshot(updated, cancellationToken);

                    if (updated.Status == SnapshotStatus.Ready)
                    {
                        provisioned++;
                        _logger.LogInformation("Provisioned snapshot {SnapshotId}.", updated.Id);
                    }
                    else if (updated.Status == SnapshotStatus.Failed)
                    {
                        _logger.LogWarning("Failed to provision snapshot {SnapshotId}.", updated.Id);
                    }
                }
            }

            if (provisioned > 0)
            {
                _logger.LogInformation("Provisioned {Provisioned} snapshot(s) this cycle.", provisioned);
            }
        }
    }
}
