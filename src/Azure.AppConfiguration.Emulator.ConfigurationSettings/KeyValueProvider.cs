// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.AppConfiguration.Emulator.Tenant;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.ConfigurationSettings
{
    public class KeyValueProvider :
        IKeyValueProvider,
        IKeyProvider,
        ILabelProvider,
        IRevisionProvider,
        IHostedService,
        IDisposable
    {
        private static readonly TimeSpan CacheScanFrequence = TimeSpan.FromMinutes(60);
        private const int MinCoalescingItems = 100;

        private readonly IKeyValueStorage _storage;
        private readonly TenantOptions _tenant;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private ReaderWriterLockAsync _lock = new();
        private List<KvIndex> _cache = null;
        private int _init;
        private bool _disposed;
        private long _scanTicks = 0;
        private int _coalesceItems;

        private static readonly IComparer<KvIndex> EqualComparer = Comparer<KvIndex>.Create(
            (a, b) =>
            {
                int result = string.Compare(a.Key, b.Key, StringComparison.Ordinal);

                if (result != 0)
                {
                    return result;
                }

                return string.Compare(a.Label, b.Label, StringComparison.Ordinal);
            });

        private static readonly IComparer<KvIndex> KeyComparer = Comparer<KvIndex>.Create(
            (a, b) => string.Compare(a.Key, b.Key, StringComparison.Ordinal));

        private struct KvIndex
        {
            public string Key { get; init; }

            public string Label { get; init; }

            public IList<KeyValue> Items { get; set; }
        }

        public KeyValueProvider(
            IKeyValueStorage storage,
            IOptions<TenantOptions> tenant)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _tenant = tenant?.Value ?? throw new ArgumentNullException(nameof(tenant));
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

        public async ValueTask<Page<KeyValue>> QueryKeyValues(
            KeyValueSearchOptions options,
            CancellationToken cancellationToken)
        {
            await EnsureInit();

            using var dispose = await _lock.ReadLock(cancellationToken);

            IEnumerable<KeyValue> items = QueryKeyValues(options);

            Page<KeyValue> page = new(items);

            //
            // Pagination
            if (page.Count() > 0 &&
                items.Count() >= _tenant.OutputPageSize)
            {
                KeyValue last = page.Last();

                page.ContinuationToken = $"{last.Key}\n{last.Label}";
            }

            //
            // Set page Etag
            page.Etag = KvHelper.ComputeEtag(page);

            return page;
        }

        public async ValueTask<KeyValue> GetKeyValue(
            string key,
            string label,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            await EnsureInit();

            using var dispose = await _lock.ReadLock(cancellationToken);

            KeyValue result = null;

            int i = _cache.BinarySearch(
                new KvIndex
                {
                    Key = key,
                    Label = label
                },
                EqualComparer);

            if (i >= 0)
            {
                result = _cache[i].Items.FirstOrDefault();
            }

            return (result?.Deleted == null) ? result : null;
        }

        public async ValueTask Remove(
            KeyValue kv,
            CancellationToken cancellationToken)
        {
            await EnsureInit();

            ValidateKeyValue(kv);

            await Save(
                new KeyValue
                {
                    Etag = kv.Etag,
                    Key = kv.Key,
                    Label = kv.Label,
                    Deleted = DateTimeOffset.UtcNow
                },
                cancellationToken);
        }

        public async ValueTask<KeyValue> Set(
            KeyValue kv,
            CancellationToken cancellationToken)
        {
            await EnsureInit();

            ValidateKeyValue(kv);

            KeyValue entry = (KeyValue)kv.Clone();

            await Save(entry, cancellationToken);

            return entry;
        }

        public async ValueTask<KeyValue> Lock(
           KeyValue kv,
           CancellationToken cancellationToken)
        {
            await EnsureInit();

            ValidateKeyValue(kv);

            KeyValue entry = (KeyValue)kv.Clone();

            entry.Locked = true;

            await Save(entry, cancellationToken);

            return entry;
        }

        public async ValueTask<KeyValue> Unlock(
            KeyValue kv,
            CancellationToken cancellationToken)
        {
            await EnsureInit();

            ValidateKeyValue(kv);

            KeyValue entry = (KeyValue)kv.Clone();

            entry.Locked = false;

            await Save(entry, cancellationToken);

            return entry;
        }

        public async ValueTask<Page<Key>> QueryKeys(
            KeySearchOptions options,
            CancellationToken cancellationToken)
        {
            await EnsureInit();

            using var dispose = await _lock.ReadLock(cancellationToken);

            IEnumerable<KeyValue> items = QueryKeyValues(
                new KeyValueSearchOptions
                {
                    KeyFilter = options.KeyFilter,
                    TimeGate = options.TimeGate,
                    ContinuationToken = options.ContinuationToken
                });

            Page<Key> page = new(items.Select(
                x => new Key
                {
                    Name = x.Key
                })
                .DistinctBy(x => x.Name));

            //
            // Pagination
            if (page.Count() > 0 &&
                items.Count() >= _tenant.OutputPageSize)
            {
                KeyValue last = items.Last();

                page.ContinuationToken = $"{last.Key}\n{last.Label}";
            }

            return page;
        }

        public async ValueTask<Page<Label>> QueryLabels(
            LabelSearchOptions options,
            CancellationToken cancellationToken)
        {
            await EnsureInit();

            using var dispose = await _lock.ReadLock(cancellationToken);

            IEnumerable<KeyValue> items = QueryKeyValues(
                new KeyValueSearchOptions
                {
                    LabelFilter = options.LabelFilter,
                    TimeGate = options.TimeGate,
                    ContinuationToken = options.ContinuationToken
                });

            Page<Label> page = new(items.Select(
                x => new Label
                {
                    Name = x.Label
                })
                .DistinctBy(x => x.Name));

            //
            // Pagination
            if (page.Count() > 0 &&
                items.Count() >= _tenant.OutputPageSize)
            {
                KeyValue last = items.Last();

                page.ContinuationToken = $"{last.Key}\n{last.Label}";
            }

            return page;
        }

        public async Task<Page<KeyValue>> QueryRevisions(
            KeyValueSearchOptions options,
            CancellationToken cancellationToken)
        {
            await EnsureInit();

            using var dispose = await _lock.ReadLock(cancellationToken);

            IEnumerable<(KeyValue item, int pos)> items = QueryRevisions(options);

            Page<KeyValue> page = new(items.Select(x => x.item));

            //
            // Pagination
            if (items.Count() >= _tenant.OutputPageSize)   // Full page reached, may have more 
            {
                var (last, pos) = items.Last();

                page.ContinuationToken = $"{last.Key}\n{last.Label}\n{pos}";
            }

            //
            // Set page Etag
            page.Etag = KvHelper.ComputeEtag(page);

            return page;
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            //
            // Start background initialization
            _ = EnsureInit()
                .AsTask()
                .ContinueWith(t =>
                {
                    //
                    // Ignore exceptions.
                    // Initialization will repeat per request if necessary
                    if (t.Exception != null)
                    {
                        t.Exception.Handle(e => true);
                    }
                },
                TaskContinuationOptions.OnlyOnFaulted);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            //
            // Cancel any background activities before existing.
            _cts.Cancel();

            return Task.CompletedTask;
        }

        private static void ValidateKeyValue(KeyValue kv)
        {
            if (kv == null)
            {
                throw new ArgumentNullException(nameof(kv));
            }

            if (string.IsNullOrEmpty(kv.Key))
            {
                throw new ArgumentNullException(nameof(kv.Key));
            }
        }

        private async ValueTask EnsureInit()
        {
            while (Interlocked.CompareExchange(ref _init, 1, 0) == 1)
            {
                await Task.Delay(100, _cts.Token);
            }

            if (_init > 1)
            {
                //
                // Scan the cache
                long ticks = Interlocked.Read(ref _scanTicks);

                if (ticks < DateTimeOffset.UtcNow.Ticks)
                {
                    _ = ScanCacheExpired(_cts.Token);
                }

                return;
            }

            try
            {
                _cache = await ReadFromStorage(_cts.Token);

                //
                // Set completed
                Interlocked.Exchange(ref _init, 2);
            }
            catch
            {
                //
                // Failed, unlock. Allowed to retry later.
                Interlocked.Exchange(ref _init, 0);

                throw;
            }
        }

        private async Task<List<KvIndex>> ReadFromStorage(
            CancellationToken cancellationToken)
        {
            List<KvIndex> entries = new();

            await foreach (KeyValue kv in _storage.QueryKeyValues(cancellationToken))
            {
                AddSorted(entries, kv);
            }

            return entries;
        }

        private IEnumerable<KeyValue> QueryKeyValues(KeyValueSearchOptions options)
        {
            Debug.Assert(options != null);

            //
            // Try the indexed query first.
            IEnumerable<KvIndex> items = IndexLookup(options);

            //
            // Filter continuation.
            if (TryParseContinuationToken(
                options.ContinuationToken,
                out string continuationKey,
                out string continuationLabel,
                out var _))
            {
                items = items.Where(x =>
                    EqualComparer.Compare(
                        x,
                        new KvIndex
                        {
                            Key = continuationKey,
                            Label = continuationLabel
                        }) > 0);
            }

            //
            // Do sequential evaluation
            return items.Select(x =>
                x.Items.FirstOrDefault(x =>
                    MatchTimeGate(x, options.TimeGate) &&
                    MatchTags(x, options.Tags)))
                .Where(x =>
                    x != null &&
                    x.Deleted == null)
                .Take(_tenant.OutputPageSize)
                .ToList();
        }

        private IEnumerable<(KeyValue, int)> QueryRevisions(KeyValueSearchOptions options)
        {
            Debug.Assert(options != null);

            List<(KeyValue item, int pos)> result = new();

            //
            // Try the indexed query.
            IEnumerable<KvIndex> items = IndexLookup(options);

            //
            // Filter continuation.
            if (TryParseContinuationToken(
                options.ContinuationToken,
                out string continuationKey,
                out string continuationLabel,
                out int continuationIndex))
            {
                items = items.Where(x =>
                    EqualComparer.Compare(
                        x,
                        new KvIndex
                        {
                            Key = continuationKey,
                            Label = continuationLabel
                        }) >= 0);
            }

            //
            // Do sequential evaluation
            foreach (KvIndex kvIndex in items)
            {
                int i = 0;

                if (kvIndex.Key == continuationKey &&
                    kvIndex.Label == continuationLabel)
                {
                    i = continuationIndex + 1;
                }

                for (i = continuationIndex; i < kvIndex.Items.Count; ++i)
                {
                    KeyValue kv = kvIndex.Items[i];

                    if (kv.Deleted == null &&
                        MatchTimeGate(kv, options.TimeGate) &&
                        MatchTags(kv, options.Tags))
                    {
                        //
                        // Check if expired
                        KeyValue last = result.LastOrDefault().item;

                        if (last != null &&
                            last.Key == kv.Key &&
                            last.Label == kv.Label &&
                            kv.Timestamp.Add(kv.RevisionTTL) < DateTimeOffset.UtcNow)
                        {
                            break;
                        }

                        result.Add((kv, i));
                    }
                }

                //
                // End of page
                if (result.Count >= _tenant.OutputPageSize)
                {
                    break;
                }
            }

            return result;
        }

        private IEnumerable<KvIndex> IndexLookup(KeyValueSearchOptions options)
        {
            Debug.Assert(options != null);

            IComparer<KvIndex> comparer = GetComparer(options.KeyFilter, options.LabelFilter);

            IEnumerable<KvIndex> items = _cache;

            //
            // Calculate continuation offset (if any)
            int continuationOffset = 0;

            if (TryParseContinuationToken(
                options.ContinuationToken,
                out string continuationKey,
                out string continuationLabel,
                out _))
            {
                int i = _cache.BinarySearch(
                    new KvIndex
                    {
                        Key = continuationKey,
                        Label = continuationLabel
                    },
                    EqualComparer);

                if (i < 0)
                {
                    i = ~i;

                    continuationOffset = i;
                }
            }

            //
            // Apply filter
            if (!options.KeyFilter.IsEmpty)
            {
                IEnumerable<string> keys = options.KeyFilter.AnyOf ?? [options.KeyFilter.EqualsTo ?? options.KeyFilter.Prefix];

                if (keys.Any())
                {
                    items = Enumerable.Empty<KvIndex>();

                    foreach (string k in keys.OrderBy(x => x))
                    {
                        int i = _cache.Count - continuationOffset;

                        while (i > 0)
                        {
                            i = _cache.BinarySearch(
                               continuationOffset,
                               i,
                               new KvIndex
                               {
                                   Key = k,
                                   Label = options.LabelFilter.EqualsTo ?? options.LabelFilter.Prefix
                               },
                               comparer);
                        }

                        items = items.Concat(
                            _cache
                                .Skip(~i)
                                .TakeWhile(x => options.KeyFilter.Match(x.Key)));
                    }
                }
            }
            else
            {
                items = items.Skip(continuationOffset);
            }

            return items
                    .Where(x =>
                        options.KeyFilter.Match(x.Key) &&
                        options.LabelFilter.Match(x.Label));
        }

        private static bool MatchTimeGate(KeyValue kv, DateTimeOffset? timeGate)
        {
            return
                timeGate == null ||
                timeGate.Value > kv.Timestamp;
        }

        private static bool MatchTags(
            KeyValue kv,
            IEnumerable<KeyValuePair<string, string>> tags)
        {
            if (tags == null)
            {
                return true;
            }

            if (kv.Tags == null)
            {
                return false;
            }

            return !tags.Any(x =>
                !kv.Tags.TryGetValue(x.Key, out string value) ||
                value != x.Value);
        }

        private static IComparer<KvIndex> GetComparer(
            StringFilter keyFilter,
            StringFilter labelFilter)
        {
            //
            // Explicit key+label
            if ((keyFilter.EqualsTo != null || keyFilter.IsNull) &&
                (labelFilter.EqualsTo != null || labelFilter.IsNull))
            {
                return EqualComparer;
            }

            //
            // Key compare
            return KeyComparer;
        }

        private async Task Save(KeyValue kv, CancellationToken cancellationToken)
        {
            Debug.Assert(kv != null);

            using IDisposable writeLock = await _lock.WriteLock(cancellationToken);

            //
            // Check for race condition
            int i = _cache.BinarySearch(
                new KvIndex
                {
                    Key = kv.Key,
                    Label = kv.Label
                },
                EqualComparer);

            KeyValue existing = null;

            if (i >= 0)
            {
                existing = _cache[i].Items.FirstOrDefault();

                if (existing?.Deleted != null)
                {
                    existing = null;
                }
            }

            if (existing?.Etag != kv.Etag)
            {
                throw new ConflictException();
            }

            //
            // Update the intrinsic entry markers
            kv.Etag = KvHelper.GenerateEtag();
            kv.Timestamp = DateTimeOffset.UtcNow;
            kv.RevisionTTL = _tenant.ConfigurationSettingRetentionPeriod;

            //
            // Append to storage
            await _storage.AppendKeyValue(kv, cancellationToken);

            //
            // Update the cache
            AddSorted(_cache, kv);
        }

        private static void AddSorted(List<KvIndex> list, KeyValue kv)
        {
            Debug.Assert(list != null);
            Debug.Assert(kv != null);

            KvIndex kvref = new KvIndex
            {
                Key = kv.Key,
                Label = kv.Label
            };

            int i = list.BinarySearch(kvref, EqualComparer);

            if (i >= 0)
            {
                kvref = list[i];
            }
            else
            {
                i = ~i;

                kvref.Items = new List<KeyValue>();

                list.Insert(i, kvref);
            }

            //
            // Use shared references
            kv.Key = kvref.Key;
            kv.Label = kvref.Label;

            kvref.Items.Insert(0, kv);
        }

        private static bool TryParseContinuationToken(
            string token,
            out string key,
            out string label,
            out int pos)
        {
            key = label = null;
            pos = 0;

            if (string.IsNullOrEmpty(token))
            {
                return false;
            }

            string[] args = token.Split('\n', 3, StringSplitOptions.None);

            if (args.Length < 2)
            {
                return false;
            }

            key = args[0];
            label = args[1];

            if (args.Length > 2)
            {
                if (!int.TryParse(args[2], out pos))
                {
                    return false;
                }
            }

            return true;
        }

        private async Task ScanCacheExpired(CancellationToken cancellationToken)
        {
            Debug.Assert(_cache != null);

            try
            {
                if (Interlocked.Increment(ref _coalesceItems) == 1 &&
                    await GetExpiredItemsCount(cancellationToken) >= MinCoalescingItems)
                {
                    await RemoveExpiredItems(cancellationToken);
                }
            }
            catch (TimeoutException)
            {
                //
                // Ignore timeout
            }
            finally
            {
                if (Interlocked.Decrement(ref _coalesceItems) == 0)
                {
                    //
                    // Set next scan
                    Interlocked.Exchange(
                        ref _scanTicks,
                        DateTimeOffset.UtcNow.Add(CacheScanFrequence).Ticks);
                }
            }
        }

        private async ValueTask<int> GetExpiredItemsCount(CancellationToken cancellationToken)
        {
            using var dispose = await _lock.ReadLock(cancellationToken);

            int expiredItems = 0;

            foreach (KvIndex kv in _cache)
            {
                for (int i = kv.Items.Count - 1; i > 0; --i)
                {
                    KeyValue item = kv.Items[i];

                    if (item.Timestamp.Add(item.RevisionTTL) < DateTimeOffset.UtcNow)
                    {
                        ++expiredItems;
                    }
                }
            }

            return expiredItems;
        }

        private async Task RemoveExpiredItems(CancellationToken cancellationToken)
        {
            using var dispose = await _lock.WriteLock(cancellationToken);

            //
            // Update the cache
            foreach (KvIndex kv in _cache)
            {
                for (int i = kv.Items.Count - 1; i > 0; --i)
                {
                    KeyValue item = kv.Items[i];

                    if (item.Timestamp.Add(item.RevisionTTL) < DateTimeOffset.UtcNow)
                    {
                        kv.Items.RemoveAt(i);
                    }
                }
            }

            //
            // Persist the changes
            await _storage.Save(
                _cache.SelectMany(x => x.Items.Reverse()),
                cancellationToken);
        }
    }
}
