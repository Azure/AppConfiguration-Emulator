// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.ConfigurationSnapshots
{
    public class SnapshotsStorage : ISnapshotsStorage, ISnapshotContentsStorage
    {
        private readonly SnapshotProviderOptions _providerOptions;
        private readonly SnapshotsStorageOptions _options;
        private readonly string _metadataFilePath;
        private readonly string _contentDirectory;

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

            _contentDirectory = _options.ContentDirectory;
            if (!Path.IsPathRooted(_contentDirectory))
            {
                _contentDirectory = Path.Combine(host.ContentRootPath, _contentDirectory);
            }

            _contentDirectory = Path.GetFullPath(_contentDirectory);

            InsureFileExist(_metadataFilePath);
            InsureDirectoryExist(_contentDirectory);
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

        public Task<Snapshot> GetSnapshot(string snapshotId, CancellationToken cancellationToken) => GetSnapshotInternal(snapshotId, cancellationToken);

        private async Task<Snapshot> GetSnapshotInternal(string snapshotId, CancellationToken cancellationToken)
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

            // Ensure initial provisioning status
            snapshot.Status = SnapshotStatus.Provisioning;
            snapshot.StatusCode = 0;
            snapshot.ItemCount = 0;
            snapshot.Size = 0;
            snapshot.LastModified = DateTimeOffset.UtcNow;
            snapshot.Media = null; // will be populated after external content build

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

        public IAsyncEnumerable<KeyValue> ReadSnapshotContent(Snapshot snapshot, long offset) => ReadSnapshotContentInternal(snapshot, offset, CancellationToken.None);

        private async IAsyncEnumerable<KeyValue> ReadSnapshotContentInternal(
            Snapshot snapshot,
            long offset,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            ValidateSnapshot(snapshot);
            if (snapshot.Status != SnapshotStatus.Ready)
            {
                yield break; // not ready: no content access
            }

            string filePath = GetContentFilePath(snapshot.Id);
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Snapshot content file not found for snapshot '{snapshot.Id}'", filePath);
            }

            using var fs = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);

            var reader = new NdJsonStreamReader<KeyValue>(
                fs,
                (ref Utf8JsonReader r, out KeyValue kv) => r.TryReadKeyValue(out kv),
                _options.ReadBufferSizeHint,
                _options.MaxReadBufferSize);

            using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_options.ReadTimeout);

            long index = 0;
            await foreach (var kv in reader.ReadItems(cts.Token))
            {
                if (index++ < offset)
                {
                    continue;
                }

                yield return kv;
            }
        }

        public async Task SaveSnapshotContent(
            Snapshot snapshot,
            IEnumerable<KeyValue> items,
            CancellationToken cancellationToken)
        {
            ValidateSnapshot(snapshot);
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            string filePath = GetContentFilePath(snapshot.Id);
            string tempFilePath = filePath + ".tmp";

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

                foreach (var kv in items)
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
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException("SaveSnapshotContent", ex);
            }

            ReplaceFile(tempFilePath, filePath);

            snapshot.Media ??= new MediaInfo();
            snapshot.Media.Category = "snapshots";
            snapshot.Media.Name = Path.GetFileName(filePath);
            snapshot.Media.ContentType = "application/x-ndjson";
            var fi = new FileInfo(filePath);
            snapshot.Size = fi.Length;
            snapshot.ItemCount = await CountLines(filePath, cancellationToken);
            snapshot.Media.Size = snapshot.ItemCount;
            snapshot.Media.Etag = SnapshotHelper.GenerateEtag();
            snapshot.Media.Sha256Hash = ComputeSha256(filePath);
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

            if (!string.IsNullOrEmpty(directoryPath) &&
                !Directory.Exists(directoryPath))
            {
                _ = Directory.CreateDirectory(directoryPath);
            }

            using FileStream fs = new FileStream(filePath, FileMode.Append);
            using StreamWriter writer = new StreamWriter(fs);
        }

        private static void InsureDirectoryExist(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        private string GetContentFilePath(string snapshotId) => Path.Combine(_contentDirectory, snapshotId + ".ndjson");

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

        private static async Task<long> CountLines(string filePath, CancellationToken cancellationToken)
        {
            long count = 0;
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new StreamReader(fs, Encoding.UTF8, true, 1024, leaveOpen: true);
            while (!reader.EndOfStream)
            {
                await reader.ReadLineAsync();
                ++count;
                cancellationToken.ThrowIfCancellationRequested();
            }

            return count;
        }

        private static byte[] ComputeSha256(string filePath)
        {
            using FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using SHA256 sha = SHA256.Create();
            return sha.ComputeHash(fs);
        }
    }
}
