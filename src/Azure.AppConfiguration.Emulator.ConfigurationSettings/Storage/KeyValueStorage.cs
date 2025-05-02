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

            await foreach (KeyValue kv in reader.ReadItems(cancellationToken))
            {
                yield return kv;
            }
        }

        public async Task AppendKeyValue(KeyValue kv, CancellationToken cancellationToken)
        {
            if (kv == null)
            {
                throw new ArgumentNullException(nameof(kv));
            }

            //
            // Open the file tp append
            using var fs = new FileStream(
                _options.FilePath,
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

            //
            // Flush
            await json.FlushAsync(cancellationToken);
            await fs.FlushAsync(cancellationToken);
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
        }
    }
}
