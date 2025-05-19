// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;

namespace Azure.AppConfiguration.Emulator.ConfigurationSettings
{
    public class NdJsonStreamReader<T>
    {
        public delegate bool Parser(ref Utf8JsonReader reader, out T item);

        private static readonly JsonReaderOptions Options = new()
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        private readonly Stream _stream;
        private readonly Parser _parser;
        private readonly int _bufferSizeHint;
        private readonly int _maxBufferSize;

        public NdJsonStreamReader(
            Stream stream,
            Parser parser,
            int bufferSizeHint,
            int maxBufferSize)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
            _bufferSizeHint = bufferSizeHint > 0 ? bufferSizeHint : throw new ArgumentOutOfRangeException(nameof(bufferSizeHint));
            _maxBufferSize = maxBufferSize >= bufferSizeHint ? maxBufferSize : throw new ArgumentOutOfRangeException(nameof(maxBufferSize));
        }

        public async IAsyncEnumerable<T> ReadItems(
            [EnumeratorCancellation]
            CancellationToken cancellationToken)
        {
            IMemoryOwner<byte> buffer = null;

            Resize(ref buffer, _bufferSizeHint);

            using (buffer as IDisposable)
            {
                int pos = 0;

                while (true)
                {
                    int bytesRead = await _stream.ReadAsync(
                        buffer.Memory.Slice(pos),
                        cancellationToken);

                    if (bytesRead <= 0)
                    {
                        yield break;
                    }

                    int totalBytesConsumed = 0;

                    ReadOnlyMemory<byte> jsonBlock = buffer.Memory.Slice(0, pos + bytesRead);

                    do
                    {
                        bool result = TryParse(
                            jsonBlock.Slice(totalBytesConsumed).Span,
                            out T item,
                            out int bytesConsumed);

                        totalBytesConsumed += bytesConsumed;

                        if (result)
                        {
                            yield return item;
                        }

                        if (bytesConsumed == 0)
                        {
                            break;
                        }
                    }
                    while (true);

                    ReadOnlyMemory<byte> unparsed = jsonBlock.Slice(totalBytesConsumed);

                    //
                    // Resize the buffer if needed
                    if (unparsed.Length > buffer.Memory.Length / 2)
                    {
                        Resize(ref buffer, buffer.Memory.Length * 3 / 2);
                    }

                    //
                    // Copy unparsed data to the beginning of the buffer
                    if (unparsed.Length > 0)
                    {
                        unparsed.CopyTo(buffer.Memory);
                    }

                    pos = unparsed.Length;
                }
            }
        }

        private bool TryParse(
            ReadOnlySpan<byte> buffer,
            out T item,
            out int bytesConsumed)
        {
            item = default;
            bytesConsumed = 0;

            if (buffer.IsEmpty)
            {
                return false;
            }

            var reader = new Utf8JsonReader(
                jsonData: buffer,
                isFinalBlock: false,
                state: new JsonReaderState(Options));

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    if (_parser(ref reader, out item))
                    {
                        if (!reader.IsTokenNull() &&
                            reader.TokenType != JsonTokenType.EndObject)
                        {
                            reader.SkipObject();
                        }

                        bytesConsumed = (int)reader.BytesConsumed;

                        return true;
                    }
                }
            }

            return false;
        }

        private void Resize(ref IMemoryOwner<byte> buffer, int size)
        {
            Debug.Assert(size > 0);

            if (size > _maxBufferSize)
            {
                throw new InvalidOperationException("Buffer size exceeded the maximum limit.");
            }

            //
            // Check if the buffer size is already sufficient
            if (buffer?.Memory.Length >= size)
            {
                return;
            }

            IDisposable current = buffer;

            //
            // Rent from the pool
            buffer = MemoryPool<byte>.Shared.Rent(size);

            //
            // Release to the pool
            current?.Dispose();
        }
    }
}
