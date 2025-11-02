// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using Azure.AppConfiguration.Emulator.Tenant;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.ConfigurationSnapshots
{
    public sealed class SnapshotProvider : ISnapshotProvider
    {
        private readonly TenantOptions _tenant;
        private readonly SnapshotProviderOptions _options;
        private readonly ISnapshotsStorage _storage;
        private readonly ISnapshotContentsStorage _contents;

        public SnapshotProvider(
            ISnapshotsStorage appConfigurationStorage,
            ISnapshotContentsStorage contents,
            IOptions<TenantOptions> tenant,
            IOptions<SnapshotProviderOptions> options)
        {
            ValidateOptions(options?.Value);

            _storage = appConfigurationStorage ?? throw new ArgumentNullException(nameof(appConfigurationStorage));
            _contents = contents ?? throw new ArgumentNullException(nameof(contents));
            _tenant = tenant?.Value ?? throw new ArgumentNullException(nameof(tenant));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task Create(Snapshot snapshot, CancellationToken cancellationToken)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            if (string.IsNullOrEmpty(snapshot.Name))
            {
                throw new ArgumentNullException(nameof(snapshot.Name));
            }

            if (snapshot.Filters == null)
            {
                throw new ArgumentNullException(nameof(snapshot.Filters));
            }

            int filterCount = snapshot.Filters.Count();
            if (filterCount < _options.MinFilterCount || filterCount > _options.MaxFilterCount)
            {
                throw new ArgumentOutOfRangeException(nameof(snapshot.Filters));
            }

            foreach (KeyValueFilter filter in snapshot.Filters)
            {
                if (snapshot.CompositionType == CompositionType.Key)
                {
                    ValidateLabelForComposeByKey(filter.Label);
                }
            }

            Debug.Assert(_tenant != null);

            var entry = new Snapshot
            {
                Id = SnapshotHelper.GenerateId(snapshot.Name, _tenant.ResourceId),
                Etag = SnapshotHelper.GenerateEtag(),
                Name = snapshot.Name,
                Filters = snapshot.Filters,
                Tags = snapshot.Tags,
                CompositionType = snapshot.CompositionType,
                RetentionPeriod = snapshot.RetentionPeriod,
                Status = SnapshotStatus.Provisioning,
                StatusCode = (int)HttpStatusCode.Accepted,
                Created = DateTimeOffset.UtcNow,
                LastModified = DateTimeOffset.UtcNow
            };

            await _storage.AddSnapshot(entry, cancellationToken);

            snapshot.Id = entry.Id;
            snapshot.Etag = entry.Etag;
            snapshot.Created = entry.Created;
            snapshot.LastModified = entry.LastModified;
            snapshot.Status = entry.Status;
            snapshot.StatusCode = entry.StatusCode;
        }

        public Task<IEnumerable<Snapshot>> Get(SnapshotSearchOptions options, CancellationToken cancellationToken)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options.Status == SnapshotStatusSearch.None)
            {
                throw new ArgumentException("At least one status is required for search.", nameof(options.Status));
            }

            throw new NotImplementedException();
        }

        public async Task<IEnumerable<KeyValue>> GetContent(Snapshot snapshot, SnapshotContentSearchOptions options, CancellationToken cancellationToken)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (snapshot.Status != SnapshotStatus.Ready && snapshot.Status != SnapshotStatus.Archived)
            {
                throw new InvalidOperationException("Snapshot is not in a servable state");
            }

            MediaInfo media = snapshot.Media;
            if (media == null)
            {
                return Enumerable.Empty<KeyValue>();
            }

            long offset;
            if (options.ContinuationToken == null)
            {
                offset = 0;
            }
            else if (!long.TryParse(options.ContinuationToken, out offset) || offset < 0 || offset >= media.Size)
            {
                return Enumerable.Empty<KeyValue>();
            }

            return await _contents.GetContent(media, offset, cancellationToken)
                                   .Take(MaxItemCount)
                                   .ToListAsync(cancellationToken);
        }

        public Task Archive(Snapshot snapshot, CancellationToken cancellationToken)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            if (snapshot.Status != SnapshotStatus.Ready)
            {
                throw new InvalidOperationException("The snapshot is not in a state that is able to be archived.");
            }

            return UpdateStatus(snapshot, SnapshotStatus.Archived, cancellationToken);
        }

        public Task Recover(Snapshot snapshot, CancellationToken cancellationToken)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            if (snapshot.Status != SnapshotStatus.Archived)
            {
                throw new InvalidOperationException("The snapshot is in an unrecoverable state.");
            }

            return UpdateStatus(snapshot, SnapshotStatus.Ready, cancellationToken);
        }

        private Task UpdateStatus(Snapshot snapshot, SnapshotStatus status, CancellationToken cancellationToken)
        {
            snapshot.Status = status;
            snapshot.Expires = (status == SnapshotStatus.Archived) ? DateTimeOffset.UtcNow + snapshot.RetentionPeriod : null;
            snapshot.Etag = SnapshotHelper.GenerateEtag();
            return Update(snapshot, cancellationToken);
        }

        private Task Update(Snapshot snapshot, CancellationToken cancellationToken)
        {
            return _storage.UpdateSnapshot(snapshot, cancellationToken);
        }

        private static void ValidateLabelForComposeByKey(string label)
        {
            if (string.IsNullOrEmpty(label))
            {
                return;
            }

            ReadOnlySpan<char> span = label.AsSpan();
            if (SearchQuery.ContainsWildcard(span) || SearchQuery.IsListSearch(span))
            {
                throw new SearchQueryException(nameof(label), "Unexpected label which could match multiple values for a key");
            }
        }

        private void ValidateOptions(SnapshotProviderOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options.OutputPageSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options.OutputPageSize));
            }

            if (options.ReadTimeout <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(options.ReadTimeout));
            }

            if (options.WriteTimeout <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(options.WriteTimeout));
            }

            if (options.RetryTimeout <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(options.RetryTimeout));
            }

            if (options.ConflictRetryTimeout <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(options.ConflictRetryTimeout));
            }
        }

        private int MaxItemCount => _options.OutputPageSize;
    }
}
