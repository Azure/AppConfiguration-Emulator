// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.AppConfiguration.Emulator.ConfigurationSettings;
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

namespace Azure.AppConfiguration.Emulator.ConfigurationSnapshots
{
    public class SnapshotsStorage : ISnapshotsStorage
    {
        private readonly SnapshotProviderOptions _providerOptions;
        private readonly SnapshotsStorageOptions _options;
        private readonly string _metadataFilePath;

        public SnapshotsStorage(
            IOptions<SnapshotProviderOptions> providerOptions,
            IOptions<SnapshotsStorageOptions> options,
            IHostingEnvironment host)
        {
            _providerOptions = providerOptions?.Value ?? throw new ArgumentNullException(nameof(providerOptions));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

            ValidateOptions(_options);

            _metadataFilePath = _options.MetadataFilePath;
            if (!Path.IsPathRooted(_metadataFilePath))
            {
                _metadataFilePath = Path.Combine(host.ContentRootPath, _metadataFilePath);
            }

            _metadataFilePath = Path.GetFullPath(_metadataFilePath);

            InsureFileExist(_metadataFilePath);
        }

        public async IAsyncEnumerable<Snapshot> QuerySnapshots(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (!File.Exists(_metadataFilePath))
            {
                yield break;
            }

            using var fs = new FileStream(
                _metadataFilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);

            var reader = new NdJsonStreamReader<Snapshot>(
                fs,
                (ref Utf8JsonReader r, out Snapshot s) => r.TryReadSnapshot(out s),
                _options.ReadBufferSizeHint,
                _options.MaxReadBufferSize);

            using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_options.ReadTimeout);

            await using IAsyncEnumerator<Snapshot> e = reader.ReadItems(cts.Token).GetAsyncEnumerator();

            while (true)
            {
                Snapshot current = null;
                try
                {
                    if (!await e.MoveNextAsync(cts.Token))
                    {
                        yield break;
                    }

                    current = e.Current;
                }
                catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
                {
                    throw new TimeoutException("QuerySnapshots", ex);
                }

                if (current != null)
                {
                    yield return current;
                }
            }
        }

        public IAsyncEnumerable<Snapshot> QuerySnapshots()
        {
            return QuerySnapshots(CancellationToken.None);
        }

        public async Task<Snapshot> GetSnapshot(string snapshotId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(snapshotId))
            {
                throw new ArgumentNullException(nameof(snapshotId));
            }

            await foreach (var s in QuerySnapshots(cancellationToken))
            {
                if (s.Id == snapshotId)
                {
                    return s;
                }
            }

            return null;
        }

        public async Task AddSnapshot(Snapshot snapshot, CancellationToken cancellationToken)
        {
            ValidateSnapshot(snapshot);

            using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_options.WriteTimeout);

            using var fs = new FileStream(
                _metadataFilePath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.Read,
                _options.AppendBufferSize);

            if (fs.Position > 0)
            {
                fs.WriteDelimiter();
            }

            using var json = new Utf8JsonWriter(fs);
            json.WriteSnapshot(snapshot);

            try
            {
                await json.FlushAsync(cts.Token);
                await fs.FlushAsync(cts.Token);
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException("AddSnapshot", ex);
            }
        }

        public async Task UpdateSnapshot(Snapshot snapshot, CancellationToken cancellationToken)
        {
            ValidateSnapshot(snapshot);
            if (string.IsNullOrEmpty(snapshot.Etag))
            {
                throw new ArgumentNullException(nameof(snapshot.Etag));
            }

            string tempFilePath = _metadataFilePath + ".tmp";

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

                await foreach (var s in QuerySnapshots(cts.Token))
                {
                    if (fs.Position > 0)
                    {
                        fs.WriteDelimiter();
                    }

                    using var json = new Utf8JsonWriter(fs);
                    json.WriteSnapshot(s.Id == snapshot.Id ? snapshot : s);
                    await json.FlushAsync(cts.Token);
                }

                await fs.FlushAsync(cts.Token);
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException("UpdateSnapshot", ex);
            }

            ReplaceFile(tempFilePath, _metadataFilePath);
        }

        private static void ValidateSnapshot(Snapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            if (string.IsNullOrEmpty(snapshot.Id))
            {
                throw new ArgumentNullException(nameof(snapshot.Id));
            }
        }

        private void ValidateOptions(SnapshotsStorageOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (string.IsNullOrEmpty(options.MetadataFilePath))
            {
                throw new ArgumentNullException(nameof(options.MetadataFilePath));
            }

            if (string.IsNullOrEmpty(options.ContentDirectory))
            {
                throw new ArgumentNullException(nameof(options.ContentDirectory));
            }

            if (options.AppendBufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options.AppendBufferSize));
            }

            if (options.WriteBufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options.WriteBufferSize));
            }

            if (options.MaxReadBufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options.MaxReadBufferSize));
            }

            if (options.ReadBufferSizeHint <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options.ReadBufferSizeHint));
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

        private static void InsureFileExist(string filePath)
        {
            Debug.Assert(!string.IsNullOrEmpty(filePath));

            if (File.Exists(filePath))
            {
                return;
            }

            string directoryPath = Path.GetDirectoryName(filePath);

            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                _ = Directory.CreateDirectory(directoryPath);
            }

            using FileStream fs = new FileStream(filePath, FileMode.Append);
            using StreamWriter writer = new StreamWriter(fs);
        }

        private static void ReplaceFile(string tempFilePath, string targetFilePath)
        {
            if (File.Exists(targetFilePath))
            {
                string bakFilePath = targetFilePath + ".bac";
                File.Replace(tempFilePath, targetFilePath, bakFilePath);
                if (File.Exists(bakFilePath)) File.Delete(bakFilePath);
            }
            else
            {
                File.Move(tempFilePath, targetFilePath);
            }

            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }
}
