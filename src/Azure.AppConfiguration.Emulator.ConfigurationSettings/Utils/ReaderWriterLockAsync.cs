using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.ConfigurationSettings
{
    public sealed class ReaderWriterLockAsync : IDisposable
    {
        private readonly SemaphoreSlim _writerLock = new(1, 1);
        private readonly SemaphoreSlim _readerLock = new(1, 1);
        private long _readers = 0;

        private readonly struct Disposable(Action action) : IDisposable
        {
            public void Dispose()
            {
                Debug.Assert(action != null);

                action();
            }
        }

        public void Dispose()
        {
            _readerLock.Dispose();
            _writerLock.Dispose();
        }

        public async ValueTask<IDisposable> ReadLock(CancellationToken cancellationToken)
        {
            await _readerLock.WaitAsync(cancellationToken);

            try
            {
                if (Interlocked.Increment(ref _readers) == 1)
                {
                    await _writerLock.WaitAsync(cancellationToken);
                }
            }
            finally
            {
                _readerLock.Release();
            }

            return new Disposable(() =>
            {
                if (Interlocked.Decrement(ref _readers) == 0)
                {
                    _writerLock.Release();
                }
            });
        }

        public async ValueTask<IDisposable> WriteLock(CancellationToken cancellationToken)
        {
            await _readerLock.WaitAsync(cancellationToken);

            try
            {
                await _writerLock.WaitAsync(cancellationToken);
            }
            catch
            {
                _readerLock.Release();

                throw;
            }

            return new Disposable(() =>
            {
                _writerLock.Release();
                _readerLock.Release();
            });
        }
    }
}
