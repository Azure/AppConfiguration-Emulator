using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.Host.Tests
{
    public class TestKeyValueStorage : IKeyValueStorage
    {
        private readonly List<KeyValue> _items = new();

        public TestKeyValueStorage()
        {
            // Pre-populate with test data
            SeedTestData();
        }

        private void SeedTestData()
        {
            // Add test key-values for GET operations
            _items.Add(new KeyValue
            {
                Key = "test-key1",
                Label = "test-label1",
                Value = "test-value1",
                Etag = "\"etag1\"",
                Timestamp = DateTimeOffset.UtcNow.AddHours(-1)
            });

            _items.Add(new KeyValue
            {
                Key = "test-key2",
                Label = "", // Empty label
                Value = "test-value2",
                Etag = "\"etag2\"",
                Timestamp = DateTimeOffset.UtcNow.AddHours(-2)
            });

            // Add a deleted key-value to test deleted item handling
            _items.Add(new KeyValue
            {
                Key = "deleted-key",
                Label = "dev",
                Etag = "\"etag3\"",
                Deleted = DateTimeOffset.UtcNow.AddHours(-3),
                Timestamp = DateTimeOffset.UtcNow.AddHours(-3)
            });

            // Add key-values with same key but different labels
            _items.Add(new KeyValue
            {
                Key = "multi-label-key",
                Label = "dev",
                Value = "dev-value",
                Etag = "\"etag4\"",
                Timestamp = DateTimeOffset.UtcNow.AddHours(-4)
            });

            _items.Add(new KeyValue
            {
                Key = "multi-label-key",
                Label = "prod",
                Value = "prod-value",
                Etag = "\"etag5\"",
                Timestamp = DateTimeOffset.UtcNow.AddHours(-5)
            });
        }

        public IAsyncEnumerable<KeyValue> QueryKeyValues(CancellationToken cancellationToken)
        {
            return _items.ToAsyncEnumerable();
        }

        public Task AppendKeyValue(KeyValue keyValue, CancellationToken cancellationToken)
        {
            _items.Add(keyValue);
            return Task.CompletedTask;
        }

        public Task Save(IEnumerable<KeyValue> keyValues, CancellationToken cancellationToken)
        {
            _items.Clear();
            _items.AddRange(keyValues);
            return Task.CompletedTask;
        }
    }

    // Extension method to convert IEnumerable to IAsyncEnumerable for testing
    public static class TestEnumerableExtensions
    {
        public static IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
        {
            return new AsyncEnumerableWrapper<T>(source);
        }

        private class AsyncEnumerableWrapper<T> : IAsyncEnumerable<T>
        {
            private readonly IEnumerable<T> _source;

            public AsyncEnumerableWrapper(IEnumerable<T> source)
            {
                _source = source;
            }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                return new AsyncEnumeratorWrapper<T>(_source.GetEnumerator());
            }
        }

        private class AsyncEnumeratorWrapper<T> : IAsyncEnumerator<T>
        {
            private readonly IEnumerator<T> _enumerator;

            public AsyncEnumeratorWrapper(IEnumerator<T> enumerator)
            {
                _enumerator = enumerator;
            }

            public T Current => _enumerator.Current;

            public ValueTask DisposeAsync()
            {
                _enumerator.Dispose();
                return ValueTask.CompletedTask;
            }

            public ValueTask<bool> MoveNextAsync()
            {
                return new ValueTask<bool>(_enumerator.MoveNext());
            }
        }
    }
}
