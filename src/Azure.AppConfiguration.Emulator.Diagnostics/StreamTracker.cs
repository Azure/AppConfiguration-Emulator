using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.Diagnostics
{
    class StreamTracker : Stream
    {
        private long _bytesWritten;

        public StreamTracker(Stream inner)
        {
            Stream = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        private Stream Stream { get; set; }

        public override bool CanRead => Stream.CanRead;

        public override bool CanSeek => Stream.CanSeek;

        public override bool CanWrite => Stream.CanWrite;

        public override bool CanTimeout => Stream.CanTimeout;

        public override long Length => _bytesWritten;

        public override long Position { get => Stream.Position; set => Stream.Position = value; }

        public override int ReadTimeout { get => Stream.ReadTimeout; set => Stream.ReadTimeout = value; }

        public override int WriteTimeout { get => Stream.WriteTimeout; set => Stream.WriteTimeout = value; }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state) => Stream.BeginRead(buffer, offset, count, callback, state);

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            IAsyncResult result = Stream.BeginWrite(buffer, offset, count, callback, state);

            _bytesWritten += count;

            return result;
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) => Stream.CopyToAsync(destination, bufferSize, cancellationToken);

        public override void Flush() => Stream.Flush();

        public override Task FlushAsync(CancellationToken cancellationToken) => Stream.FlushAsync(cancellationToken);

        public override int Read(byte[] buffer, int offset, int count) => Stream.Read(buffer, offset, count);

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => Stream.ReadAsync(buffer, offset, count, cancellationToken);

        public override int ReadByte() => Stream.ReadByte();

        public override long Seek(long offset, SeekOrigin origin) => Stream.Seek(offset, origin);

        public override void SetLength(long value) => Stream.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count)
        {
            Stream.Write(buffer, offset, count);

            _bytesWritten += count;
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Task result = Stream.WriteAsync(buffer, offset, count, cancellationToken);

            _bytesWritten += count;

            return result;
        }

        public override void WriteByte(byte value)
        {
            Stream.WriteByte(value);

            _bytesWritten += 1;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            Stream = null;
        }
    }
}
