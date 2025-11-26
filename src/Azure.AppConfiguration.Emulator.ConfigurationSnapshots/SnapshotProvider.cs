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
        private readonly ISnapshotContentsStorage _contentsStorage;
        private readonly IKeyValueProvider _kvProvider;

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private ReaderWriterLockAsync _lock = new();
        private List<Snapshot> _cache = null; // Sorted by Name
        private int _init;
        private bool _disposed;

        public SnapshotProvider(
            ISnapshotsStorage appConfigurationStorage,
            ISnapshotContentsStorage contentsStorage,
            IKeyValueProvider kvProvider,
            IOptions<TenantOptions> tenant,
            IOptions<SnapshotProviderOptions> options)
        {
            ValidateOptions(options?.Value);

            _storage = appConfigurationStorage ?? throw new ArgumentNullException(nameof(appConfigurationStorage));
            _contentsStorage = contentsStorage ?? throw new ArgumentNullException(nameof(contentsStorage));
            _kvProvider = kvProvider ?? throw new ArgumentNullException(nameof(kvProvider));
            _tenant = tenant?.Value ?? throw new ArgumentNullException(nameof(tenant));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public void Dispose()
        {
            //
            // Dispose can be invoked multiple times
            // Because it can be used as DI service, as well as HostedService
            if (_disposed)
            {
                return;
            }

            _cts.Cancel();

            _cts.Dispose();

            _lock.Dispose();

            _disposed = true;
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

            await EnsureInit();

            using IDisposable writeLock = await _lock.WriteLock(cancellationToken);

            // Enforce uniqueness by name
            if (_cache.Any(s => s.Name == snapshot.Name))
            {
                throw new ConflictException();
            }

            string resourceId = string.IsNullOrEmpty(_tenant.ResourceId) ? "test-resource" : _tenant.ResourceId;

            var entry = new Snapshot
            {
                Id = SnapshotHelper.GenerateId(snapshot.Name, resourceId),
                Etag = SnapshotHelper.GenerateEtag(),
                Name = snapshot.Name,
                Filters = snapshot.Filters,
                Tags = snapshot.Tags,
                CompositionType = snapshot.CompositionType,
                RetentionPeriod = snapshot.RetentionPeriod,
                Status = SnapshotStatus.None,
                StatusCode = (int)HttpStatusCode.OK,
                Created = DateTimeOffset.UtcNow,
                LastModified = DateTimeOffset.UtcNow
            };

            // Collect items according to filters synchronously
            IEnumerable<KeyValue> items = await GetItemsAsync(entry, cancellationToken);

            try
            {
                string filePath = BuildSnapshotContentFileName(entry.Id);
                MediaInfo media = await _contentsStorage.CreateContent(filePath, items, cancellationToken);

                entry.Media = media;
                entry.ItemCount = items.Count();
                entry.Size = media.Size;
                entry.Status = SnapshotStatus.Ready;
                entry.StatusCode = (int)HttpStatusCode.OK;
                entry.LastModified = DateTimeOffset.UtcNow;

                await _storage.AddSnapshot(entry, cancellationToken);

                AddSorted(_cache, entry);
            }
            catch (Exception)
            {
                entry.Status = SnapshotStatus.Failed;
                entry.StatusCode = (int)HttpStatusCode.InternalServerError;
                entry.LastModified = DateTimeOffset.UtcNow;

                await _storage.AddSnapshot(entry, cancellationToken);

                AddSorted(_cache, entry);

                throw;
            }
        }

        public async Task<IEnumerable<Snapshot>> Get(SnapshotSearchOptions options, CancellationToken cancellationToken)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options.Status == SnapshotStatusSearch.None)
            {
                throw new ArgumentException("At least one status is required for search.", nameof(options.Status));
            }

            await EnsureInit();

            // Always refresh cache from storage before serving results to reflect latest updates
            await RefreshCacheAsync(cancellationToken);

            using IDisposable readLock = await _lock.ReadLock(cancellationToken);

            IEnumerable<Snapshot> items = _cache;

            if (!string.IsNullOrEmpty(options.Name))
            {
                items = items.Where(s => s.Name == options.Name);
            }

            items = items.Where(s => MatchStatus(options.Status, s.Status));

            // Continuation token (snapshot name)
            if (!string.IsNullOrEmpty(options.ContinuationToken))
            {
                string token = options.ContinuationToken;
                items = items.Where(s => string.Compare(s.Name, token, StringComparison.Ordinal) > 0);
            }

            // Pagination
            items = items
                .OrderBy(s => s.Name, StringComparer.Ordinal)
                .Take(_tenant.OutputPageSize)
                .ToList();

            return items;
        }

        private async Task RefreshCacheAsync(CancellationToken cancellationToken)
        {
            using IDisposable writeLock = await _lock.WriteLock(cancellationToken);

            var entries = new List<Snapshot>();
            await foreach (Snapshot s in _storage.QuerySnapshots().WithCancellation(cancellationToken))
            {
                if (s == null)
                {
                    continue;
                }

                AddSorted(entries, s);
            }

            _cache = entries;
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

            // Only Ready snapshots expose content. Any other state returns an empty page.
            if (snapshot.Status != SnapshotStatus.Ready)
            {
                return new ConfigurationSettings.Page<KeyValue>(Enumerable.Empty<KeyValue>())
                {
                    Offset = 0,
                    TotalItemsCount = 0,
                    Etag = KvHelper.GenerateEtag()
                };
            }

            MediaInfo media = snapshot.Media;
            if (media == null)
            {
                var emptyPage = new ConfigurationSettings.Page<KeyValue>(Enumerable.Empty<KeyValue>())
                {
                    Offset = 0,
                    TotalItemsCount = 0,
                    Etag = KvHelper.GenerateEtag()
                };

                return emptyPage;
            }

            long offset;
            if (options.ContinuationToken == null)
            {
                offset = 0;
            }
            else if (!long.TryParse(options.ContinuationToken, out offset) || offset < 0 || offset >= snapshot.ItemCount)
            {
                var emptyPage = new ConfigurationSettings.Page<KeyValue>(Enumerable.Empty<KeyValue>())
                {
                    Offset = snapshot.ItemCount,
                    TotalItemsCount = snapshot.ItemCount,
                    Etag = KvHelper.GenerateEtag()
                };

                return emptyPage;
            }

            List<KeyValue> kvs = await _contentsStorage
                .GetContent(media, offset, cancellationToken)
                .Take(_options.OutputPageSize)
                .ToListAsync(cancellationToken);

            var page = new ConfigurationSettings.Page<KeyValue>(kvs)
            {
                Offset = offset,
                TotalItemsCount = snapshot.ItemCount
            };

            if (offset + kvs.Count < snapshot.ItemCount)
            {
                page.ContinuationToken = (offset + kvs.Count).ToString();
            }

            page.Etag = KvHelper.ComputeEtag(page);

            return page;
        }

        public async Task Archive(Snapshot snapshot, CancellationToken cancellationToken)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            if (snapshot.Status != SnapshotStatus.Ready)
            {
                throw new InvalidOperationException("The snapshot is not in a state that is able to be archived.");
            }

            await UpdateStatus(snapshot, SnapshotStatus.Archived, cancellationToken);
        }

        public async Task Recover(Snapshot snapshot, CancellationToken cancellationToken)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            if (snapshot.Status != SnapshotStatus.Archived)
            {
                throw new InvalidOperationException("The snapshot is in an unrecoverable state.");
            }

            await UpdateStatus(snapshot, SnapshotStatus.Ready, cancellationToken);
        }

        private async Task UpdateStatus(Snapshot snapshot, SnapshotStatus status, CancellationToken cancellationToken)
        {
            await EnsureInit();

            using IDisposable writeLock = await _lock.WriteLock(cancellationToken);

            Snapshot existing = FindByName(snapshot.Name);
            if (existing == null)
            {
                throw new SnapshotNotFoundException();
            }

            if (existing.Etag != snapshot.Etag)
            {
                throw new ConflictException();
            }

            existing.Status = status;

            if (status == SnapshotStatus.Archived)
            {
                //
                // Archive

                existing.Expires = DateTimeOffset.UtcNow + existing.RetentionPeriod;
            }
            else
            {
                Debug.Assert(status == SnapshotStatus.Ready);

                //
                // Recover

                existing.Expires = null;
            }

            existing.LastModified = DateTimeOffset.UtcNow;
            existing.Etag = SnapshotHelper.GenerateEtag();

            await _storage.UpdateSnapshot(existing, cancellationToken);
        }

        private async Task<IEnumerable<KeyValue>> GetItemsAsync(Snapshot snapshot, CancellationToken cancellationToken)
        {
            if (snapshot.Filters == null)
            {
                return Enumerable.Empty<KeyValue>();
            }

            var result = new List<KeyValue>();
            foreach (KeyValueFilter f in snapshot.Filters)
            {
                var keyFilter = SearchQuery.CreateStringFilter(f.Key);
                var labelFilter = f.Label == null ? new StringFilter { IsNull = true } : SearchQuery.CreateStringFilter(f.Label);
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

        private static string BuildSnapshotContentFileName(string snapshotId)
        {
            return snapshotId + ".ndjson";
        }

        private Snapshot FindByName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            int i = _cache.BinarySearch(new Snapshot { Name = name }, NameComparer.Instance);
            if (i >= 0)
            {
                return _cache[i];
            }

            return null;
        }

        private static bool MatchStatus(SnapshotStatusSearch searchFlags, SnapshotStatus status)
        {
            if (searchFlags == SnapshotStatusSearch.All)
            {
                return true;
            }

            if (status == SnapshotStatus.Ready && (searchFlags & SnapshotStatusSearch.Ready) != 0)
            {
                return true;
            }

            if (status == SnapshotStatus.Archived && (searchFlags & SnapshotStatusSearch.Archived) != 0)
            {
                return true;
            }

            if (status == SnapshotStatus.Failed && (searchFlags & SnapshotStatusSearch.Failed) != 0)
            {
                return true;
            }

            return false;
        }

        private async ValueTask EnsureInit()
        {
            while (Interlocked.CompareExchange(ref _init, 1, 0) == 1)
            {
                await Task.Delay(100, _cts.Token);
            }

            if (_init > 1)
            {
                return;
            }

            try
            {
                List<Snapshot> entries = new List<Snapshot>();

                await foreach (Snapshot s in _storage.QuerySnapshots())
                {
                    AddSorted(entries, s);
                }

                _cache = entries;

                Interlocked.Exchange(ref _init, 2);
            }
            catch
            {
                Interlocked.Exchange(ref _init, 0);
                throw;
            }
        }

        private static void AddSorted(List<Snapshot> list, Snapshot snapshot)
        {
            Debug.Assert(list != null);
            Debug.Assert(snapshot != null);

            int i = list.BinarySearch(snapshot, NameComparer.Instance);
            if (i >= 0)
            {
                // Replace existing (newer copy)
                list[i] = snapshot;
            }
            else
            {
                i = ~i;
                list.Insert(i, snapshot);
            }
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

        private sealed class NameComparer : IComparer<Snapshot>
        {
            public static readonly NameComparer Instance = new NameComparer();
            public int Compare(Snapshot x, Snapshot y)
            {
                string a = x?.Name;
                string b = y?.Name;
                return string.Compare(a, b, StringComparison.Ordinal);
            }
        }
    }
}
