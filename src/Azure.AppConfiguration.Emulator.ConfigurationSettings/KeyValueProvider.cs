// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.AppConfiguration.Emulator.Tenant;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.ConfigurationSettings
{
    public class KeyValueProvider :
        IKeyValueProvider,
        IKeyProvider,
        ILabelProvider,
        IDisposable
    {
        private readonly IKeyValueStorage _storage;
        private readonly TenantOptions _tenant;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private List<KvRef> _entries = null;
        private int _init;

        private static readonly IComparer<KvRef> EqualComparer = Comparer<KvRef>.Create(
            (a, b) =>
            {
                int result = string.Compare(a.Key, b.Key, StringComparison.Ordinal);

                if (result != 0)
                {
                    return result;
                }

                return string.Compare(a.Label, b.Label, StringComparison.Ordinal);
            });

        struct KvRef
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

        public async ValueTask<Page<KeyValue>> Get(
            KeyValueSearchOptions options,
            CancellationToken cancellationToken)
        {
            await EnsureInit();

            IEnumerable<KeyValue> items = QueryKeyValue(options)
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

        public ValueTask<KeyValue> Get(
            string key,
            string label,
            CancellationToken cancellationToken)
        {
            return ValueTask.FromResult<KeyValue>(null);
        }

        public ValueTask<IEnumerable<Key>> Get(
            KeySearchOptions options,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public ValueTask<IEnumerable<Label>> Get(
            LabelSearchOptions options,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public ValueTask Remove(
            KeyValue kv,
            CancellationToken cancellationToken)
        {
            ValidateKeyValue(kv);

            kv.Deleted = DateTimeOffset.UtcNow;
            kv.Value = null;

            return Set(kv, cancellationToken);
        }

        public async ValueTask Set(
            KeyValue kv,
            CancellationToken cancellationToken)
        {
            ValidateKeyValue(kv);

            kv.Etag = KvHelper.GenerateEtag();
            kv.Created = DateTimeOffset.UtcNow;
            kv.RevisionTTL = _tenant.ConfigurationSettingRetentionPeriod;

            await _storage.AppendKeyValue(kv, cancellationToken);
        }

        public ValueTask Lock(
           KeyValue kv,
           CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public ValueTask Unlock(
            KeyValue kv,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
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
                _entries = await ReadFromStorage(_cts.Token);

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

        private async Task<List<KvRef>> ReadFromStorage(
            CancellationToken cancellationToken)
        {
            List<KvRef> entries = new();

            //
            // Read from storage
            await foreach (KeyValue kv in _storage.QueryKeyValues(cancellationToken))
            {
                var kvref = new KvRef
                {
                    Key = kv.Key,
                    Label = kv.Label
                };

                int i = entries.BinarySearch(kvref, EqualComparer);

                if (i < 0)
                {
                    i = ~i;

                    kvref.Items = new List<KeyValue>();

                    entries.Insert(i, kvref);
                }

                kvref = entries.ElementAt(i);

                //
                // Reference the same objects
                kv.Key = kvref.Key;
                kv.Label = kvref.Label;

                kvref.Items.Insert(0, kv);
            }

            return entries;
        }

        private IEnumerable<KeyValue> QueryKeyValue(KeyValueSearchOptions options)
        {

            return _entries
                .Where(x => MatchFilters(x.Key, x.Label, options))
                .Select(x => x.Items.FirstOrDefault())
                .Where(x => x != null);
        }

        private static bool MatchFilters(
            string key,
            string label,
            KeyValueSearchOptions options)
        {
            //
            // Key filter
            if (!options.KeyFilter.Match(key))
            {
                return false;
            }

            //
            // Label filter
            if (!options.LabelFilter.Match(label))
            {
                return false;
            }

            return true;
        }

        private static IComparer<KvRef> CreateKeyPrefixComparer(string prefix)
        {
            Debug.Assert(!string.IsNullOrEmpty(prefix));

            return Comparer<KvRef>.Create(
                (a, b) =>
                {
                    if (a.Key != null && a.Key.StartsWith(prefix))
                    {
                        return 0;
                    }

                    if (b.Key != null && b.Key.StartsWith(prefix))
                    {
                        return 0;
                    }

                    return string.Compare(a.Key, b.Key, StringComparison.Ordinal);
                });
        }
    }
}
