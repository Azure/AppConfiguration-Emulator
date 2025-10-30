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
        private readonly ILogger<SnapshotsService> _logger;
        private static readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(10);

        public SnapshotsService(ISnapshotsStorage storage, ILogger<SnapshotsService> logger)
        {
            if (storage == null)
            {
                throw new ArgumentNullException(nameof(storage));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _storage = storage;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SnapshotsService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    int provisioningCount = 0;
                    int readyCount = 0;
                    int archivedCount = 0;
                    int failedCount = 0;

                    await foreach (var snapshot in _storage.QuerySnapshots().WithCancellation(stoppingToken))
                    {
                        if (snapshot == null)
                        {
                            continue;
                        }

                        if (snapshot.Status == SnapshotStatus.Provisioning)
                        {
                            provisioningCount++;
                        }
                        else if (snapshot.Status == SnapshotStatus.Ready)
                        {
                            readyCount++;
                        }
                        else if (snapshot.Status == SnapshotStatus.Archived)
                        {
                            archivedCount++;
                        }
                        else if (snapshot.Status == SnapshotStatus.Failed)
                        {
                            failedCount++;
                        }
                    }

                    if (provisioningCount > 0)
                    {
                        _logger.LogDebug("Provisioning snapshots detected: {ProvisioningCount}", provisioningCount);
                    }

                    _logger.LogInformation("Snapshot status counts: provisioning={ProvisioningCount}, ready={ReadyCount}, archived={ArchivedCount}, failed={FailedCount}", provisioningCount, readyCount, archivedCount, failedCount);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Graceful shutdown; swallow.
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
    }
}
