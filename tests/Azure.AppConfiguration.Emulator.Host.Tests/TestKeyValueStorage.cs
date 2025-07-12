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

            // Add key-values with tags
            _items.Add(new KeyValue
            {
                Key = "tagged-key1",
                Label = "dev",
                Value = "tagged-value1",
                Etag = "\"etag8\"",
                Timestamp = DateTimeOffset.UtcNow.AddHours(-8),
                Tags = new Dictionary<string, string>
                {
                    { "environment", "development" },
                    { "owner", "team1" }
                }
            });

            _items.Add(new KeyValue
            {
                Key = "tagged-key2",
                Label = "prod",
                Value = "tagged-value2",
                Etag = "\"etag9\"",
                Timestamp = DateTimeOffset.UtcNow.AddHours(-9),
                Tags = new Dictionary<string, string>
                {
                    { "environment", "production" },
                    { "owner", "team2" }
                }
            });

            // Add key-values with hierarchical keys
            _items.Add(new KeyValue
            {
                Key = "app1:feature1:setting1",
                Label = "dev",
                Value = "hierarchical-value1",
                Etag = "\"etag10\"",
                Timestamp = DateTimeOffset.UtcNow.AddHours(-10)
            });

            _items.Add(new KeyValue
            {
                Key = "app1:feature1:setting2",
                Label = "dev",
                Value = "hierarchical-value2",
                Etag = "\"etag11\"",
                Timestamp = DateTimeOffset.UtcNow.AddHours(-11)
            });

            _items.Add(new KeyValue
            {
                Key = "app1:feature2:setting1",
                Label = "prod",
                Value = "hierarchical-value3",
                Etag = "\"etag12\"",
                Timestamp = DateTimeOffset.UtcNow.AddHours(-12)
            });

            // Add keys specifically for filter tests
            _items.Add(new KeyValue
            {
                Key = "filtered-key1",
                Label = "filtered-label1",
                Value = "filtered-value1",
                Etag = "\"etag-filter1\"",
                Timestamp = DateTimeOffset.UtcNow.AddHours(-15)
            });

            _items.Add(new KeyValue
            {
                Key = "filtered-key2",
                Label = "filtered-label1",
                Value = "filtered-value2",
                Etag = "\"etag-filter2\"",
                Timestamp = DateTimeOffset.UtcNow.AddHours(-16)
            });

            _items.Add(new KeyValue
            {
                Key = "filtered-key3",
                Label = "filtered-label2",
                Value = "filtered-value3",
                Etag = "\"etag-filter3\"",
                Timestamp = DateTimeOffset.UtcNow.AddHours(-17)
            });

            // Add key for deletion tests
            _items.Add(new KeyValue
            {
                Key = "test-key-to-delete",
                Label = "test-label-to-delete",
                Value = "value-to-delete",
                Etag = "\"etag-delete\"",
                Timestamp = DateTimeOffset.UtcNow.AddHours(-18)
            });

            // Add more deleted key-values
            _items.Add(new KeyValue
            {
                Key = "deleted-key2",
                Label = "prod",
                Etag = "\"etag13\"",
                Deleted = DateTimeOffset.UtcNow.AddHours(-13),
                Timestamp = DateTimeOffset.UtcNow.AddHours(-13)
            });

            _items.Add(new KeyValue
            {
                Key = "app1:feature3:setting1",
                Label = "dev",
                Etag = "\"etag14\"",
                Deleted = DateTimeOffset.UtcNow.AddHours(-14),
                Timestamp = DateTimeOffset.UtcNow.AddHours(-14)
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
