// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.ConfigurationSettings
{
    /// <summary>
    /// Storage provider
    /// Data format: NDJSON - Newline delimited JSON
    /// https://github.com/ndjson/ndjson-spec
    /// </summary>
    public class KeyValueStorage : IKeyValueStorage
    {
        private readonly KeyValueStorageOptions _options;
        private readonly string _filePath;

        public KeyValueStorage(
            IOptions<KeyValueStorageOptions> options,
            IHostingEnvironment host)
        {
            ValidateOptions(options?.Value);

            _options = options.Value;

            //
            // Root the file path
            _filePath = _options.FilePath;

            if (!Path.IsPathRooted(_filePath))
            {
                _filePath = Path.Combine(host.ContentRootPath, _filePath);
            }

            _filePath = Path.GetFullPath(_filePath);

            InsureFileExist(_filePath);
        }

        public async IAsyncEnumerable<KeyValue> QueryKeyValues(
            [EnumeratorCancellation]
            CancellationToken cancellationToken)
        {
            if (!File.Exists(_filePath))
            {
                yield break;
            }

            //
            // Open the file for reading
            using var fs = new FileStream(
                _filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);

            var reader = new NdJsonStreamReader<KeyValue>(
                fs,
                (ref Utf8JsonReader reader, out KeyValue kv) => reader.TryReadKeyValue(out kv),
                _options.ReadBufferSizeHint,
                _options.MaxReadBufferSize);

            using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            cts.CancelAfter(_options.ReadTimeout);

            await using IAsyncEnumerator<KeyValue> enumerator = reader
                .ReadItems(cts.Token)
                .GetAsyncEnumerator();

            KeyValue current = null;

            while (true)
            {
                try
                {
                    if (!await enumerator.MoveNextAsync(cts.Token))
                    {
                        yield break;
                    }

                    current = enumerator.Current;
                }
                catch (OperationCanceledException e) when (!cancellationToken.IsCancellationRequested)
                {
                    throw new TimeoutException("QueryKeyValues", e);
                }

                yield return current;
            }
        }

        public async Task AppendKeyValue(
            KeyValue kv,
            CancellationToken cancellationToken)
        {
            if (kv == null)
            {
                throw new ArgumentNullException(nameof(kv));
            }

            using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            cts.CancelAfter(_options.WriteTimeout);

            //
            // Open the file to append
            using var fs = new FileStream(
                _filePath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.Read,
                _options.AppendBufferSize);

            if (fs.Position > 0)
            {
                fs.WriteDelimiter();
            }

            //
            // Write the key value
            using var json = new Utf8JsonWriter(fs);

            json.WriteKeyValue(kv);

            try
            {
                await json.FlushAsync(cts.Token);
                await fs.FlushAsync(cts.Token);
            }
            catch (OperationCanceledException e) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException("AppendKeyValue", e);
            }
        }

        public async Task Save(
            IEnumerable<KeyValue> keyValues,
            CancellationToken cancellationToken)
        {
            if (keyValues == null)
            {
                throw new ArgumentNullException(nameof(keyValues));
            }

            //
            // Use a temporary file
            string tempFilePath = $"{_filePath}.tmp";

            try
            {
                using var fs = new FileStream(
                    tempFilePath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    _options.WriteBufferSize);

                using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                cts.CancelAfter(_options.WriteTimeout);

                //
                // Write the payload
                foreach (KeyValue kv in keyValues)
                {
                    if (fs.Position > 0)
                    {
                        fs.WriteDelimiter();
                    }

                    using var json = new Utf8JsonWriter(fs);

                    json.WriteKeyValue(kv);

                    await json.FlushAsync(cts.Token);
                }

                await fs.FlushAsync(cts.Token);
            }
            catch (OperationCanceledException e) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException("Save", e);
            }

            //
            // Replace the original file with the temporary file
            if (File.Exists(_filePath))
            {
                string bacFilePath = $"{_filePath}.bac";

                File.Replace(tempFilePath, _filePath, bacFilePath);

                //
                // Clean up the bak file
                if (File.Exists(bacFilePath))
                {
                    File.Delete(bacFilePath);
                }
            }
            else
            {
                //
                // Rename the temporary file to the original file
                File.Move(tempFilePath, _filePath);
            }

            //
            // Clean up the temporary file
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }

        private static void InsureFileExist(string filePath)
        {
            Debug.Assert(!string.IsNullOrEmpty(filePath));

            if (File.Exists(filePath))
            {
                return;
            }

            //
            // Ensure the directory exists
            string directoryPath = Path.GetDirectoryName(filePath);

            if (!string.IsNullOrEmpty(directoryPath) &&
                !Directory.Exists(directoryPath))
            {
                _ = Directory.CreateDirectory(directoryPath);
            }

            //
            // Create an empty file
            using FileStream fs = new FileStream(filePath, FileMode.Append);
            using StreamWriter writer = new StreamWriter(fs);
        }

        private void ValidateOptions(KeyValueStorageOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (string.IsNullOrEmpty(options.FilePath))
            {
                throw new ArgumentNullException(nameof(options.FilePath));
            }

            if (options.AppendBufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options.AppendBufferSize));
            }

            if (options.MaxReadBufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options.MaxReadBufferSize));
            }

            if (options.ReadTimeout <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(options.ReadTimeout));
            }

            if (options.WriteTimeout <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(options.WriteTimeout));
            }
        }
    }
}
