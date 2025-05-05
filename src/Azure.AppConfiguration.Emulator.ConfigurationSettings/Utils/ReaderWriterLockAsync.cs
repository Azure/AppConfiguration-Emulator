using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.ConfigurationSettings
{
    public sealed class ReaderWriterLockAsync : IDisposable
    {
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly ReaderWriterLockSlim _lock = new();
        private long _readers = 0;

        private readonly struct DisposableLock(Action action) : IDisposable
        {
            public void Dispose()
            {
                Debug.Assert(action != null);

                action();
            }
        }

        public void Dispose()
        {
            _lock.Dispose();

            _semaphore.Dispose();
        }

        public async Task<IDisposable> ReadLock(CancellationToken cancellationToken)
        {
            _lock.EnterReadLock();

            try
            {
                if (Interlocked.Increment(ref _readers) == 1)
                {
                    await _semaphore.WaitAsync(cancellationToken);
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }

            return new DisposableLock(() =>
            {
                if (Interlocked.Decrement(ref _readers) == 0)
                {
                    _semaphore.Release();
                }
            });
        }

        public async Task<IDisposable> WriteLock(CancellationToken cancellationToken)
        {
            _lock.EnterWriteLock();

            try
            {
                await _semaphore.WaitAsync(cancellationToken);
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            return new DisposableLock(() => _semaphore.Release());
        }
    }
}
