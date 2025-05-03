using System;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.ConfigurationSettings
{
    public class ReaderWriterLockAsync : IDisposable
    {
        private readonly SemaphoreSlim _lock = new(1, 1);

        private long _readers = 0;

        private struct DisposableLock : IDisposable
        {
            private readonly Action _disposeAction;

            public DisposableLock(Action action)
            {
                _disposeAction = action ?? throw new ArgumentNullException();
            }

            public void Dispose()
            {
                _disposeAction();
            }
        }

        public void Dispose()
        {
            _lock.Dispose();
        }

        public async Task<IDisposable> ReadLock(CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref _readers) == 0)
            {
                await _lock.WaitAsync(cancellationToken);
            }

            return new DisposableLock(() =>
            {
                if (Interlocked.Decrement(ref _readers) == 0)
                {
                    _lock.Release();
                }
            });
        }

        public async Task<IDisposable> WriteLock(CancellationToken cancellationToken)
        {
            await _lock.WaitAsync(cancellationToken);

            return new DisposableLock(() => _lock.Release());
        }
    }
}
