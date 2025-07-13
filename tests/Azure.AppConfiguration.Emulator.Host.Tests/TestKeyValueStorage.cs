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
