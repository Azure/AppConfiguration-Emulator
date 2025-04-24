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
        private readonly IKeyValueStorage _storage;
        private readonly TenantOptions _tenant;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private ReaderWriterLockAsync _lock = new();
        private List<KvIndex> _cache = null;
        private int _init;

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

        struct KvIndex
        {
            public string Key { get; set; }

            public string Label { get; set; }

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
            _cts.Cancel();

            _cts.Dispose();
        }

        public async ValueTask<Page<KeyValue>> QueryKeyValues(
            KeyValueSearchOptions options,
            CancellationToken cancellationToken)
        {
            await EnsureInit();

            using var dispose = _lock.ReadLock(cancellationToken);

            IEnumerable<KeyValue> items = QueryKeyValues(options)
                .Take(_tenant.OutputPageSize)
                .ToList();

            Page<KeyValue> page;

            if (options.Range != null)
            {
                page = items.TakeRange(options.Range.Value);
            }
            else
            {
                page = new Page<KeyValue>(items);
            }

            //
            // Pagination
            if (page.Count() > 0 &&
                (items.Count() >= _tenant.OutputPageSize ||  // Full page reached, may have more 
                 page.Last().Etag != items.Last().Etag))     // Range doesn't reach end of page
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
            await EnsureInit();

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            using var dispose = _lock.ReadLock(cancellationToken);

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

            throw new NotImplementedException();
        }

        public async ValueTask<Page<Label>> QueryLabels(
            LabelSearchOptions options,
            CancellationToken cancellationToken)
        {
            await EnsureInit();

            throw new NotImplementedException();
        }

        public async Task<Page<KeyValue>> QueryRevisions(
            KeyValueSearchOptions options,
            CancellationToken cancellationToken)
        {
            await EnsureInit();

            throw new NotImplementedException();
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
                // Failed, unlock
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
            // Then sequential evaluation
            return IndexSearch(options)
                .Where(x =>
                    options.KeyFilter.Match(x.Key) &&
                    options.LabelFilter.Match(x.Label))
                .Select(x =>
                    x.Items.FirstOrDefault(x =>
                        MatchTimeGate(x, options.TimeGate) &&
                        MatchTags(x, options.Tags)))
                .Where(x =>
                    x != null &&
                    x.Deleted == null);
        }

        private IEnumerable<KvIndex> IndexSearch(KeyValueSearchOptions options)
        {
            Debug.Assert(options != null);

            IComparer<KvIndex> comparer = GetComparer(options.KeyFilter, options.LabelFilter);

            IEnumerable<KvIndex> items = Enumerable.Empty<KvIndex>();

            IEnumerable<string> keys = options.KeyFilter.AnyOf ?? [options.KeyFilter.EqualsTo ?? options.KeyFilter.Prefix];

            foreach (string key in keys.OrderBy(x => x))
            {
                int i = _cache.BinarySearch(
                    new KvIndex()
                    {
                        Key = key,
                        Label = options.LabelFilter.EqualsTo ?? options.LabelFilter.Prefix
                    },
                    comparer);

                if (i < 0)
                {
                    i = ~i;
                }

                items = items.Concat(
                    _cache
                        .Skip(i)
                        .TakeWhile(x => options.KeyFilter.Match(x.Key)));
            }

            return items;
        }

        private static bool MatchTimeGate(KeyValue kv, DateTimeOffset? timeGate)
        {
            return
                timeGate == null ||
                timeGate.Value > kv.Created;
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

            var kvref = new KvIndex
            {
                Key = kv.Key,
                Label = kv.Label
            };

            using IDisposable writeLock = await _lock.WriteLock(cancellationToken);

            //
            // Check for race condition
            int i = _cache.BinarySearch(kvref, EqualComparer);

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
            kv.Created = DateTimeOffset.UtcNow;
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
    }
}
