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
        private readonly ISnapshotsManager _manager;
        private readonly ILogger<SnapshotsService> _logger;
        private static readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(10);

        public SnapshotsService(
            ISnapshotsStorage storage,
            ISnapshotsManager manager,
            ILogger<SnapshotsService> logger)
        {
            if (storage == null)
            {
                throw new ArgumentNullException(nameof(storage));
            }

            if (manager == null)
            {
                throw new ArgumentNullException(nameof(manager));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _storage = storage;
            _manager = manager;
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

                var beforeStatus = snapshot.Status;
                await _manager.Provision(snapshot, cancellationToken);

                if (beforeStatus == SnapshotStatus.Provisioning && snapshot.Status == SnapshotStatus.Ready)
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
    }
}
