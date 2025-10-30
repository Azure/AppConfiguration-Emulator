// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.ConfigurationSnapshots
{
    public sealed class SnapshotContentsStorage : ISnapshotContentsStorage
    {
        private readonly IKeyValueProvider _keyValueProvider;
        private readonly SnapshotsStorageOptions _options;
        private readonly string _contentDirectory;

        public SnapshotContentsStorage(
            IKeyValueProvider keyValueProvider,
            IOptions<SnapshotsStorageOptions> options,
            IHostingEnvironment host)
        {
            _keyValueProvider = keyValueProvider ?? throw new ArgumentNullException(nameof(keyValueProvider));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            if (host == null) throw new ArgumentNullException(nameof(host));

            _contentDirectory = _options.ContentDirectory;
            if (!Path.IsPathRooted(_contentDirectory))
            {
                _contentDirectory = Path.Combine(host.ContentRootPath, _contentDirectory);
            }

            _contentDirectory = Path.GetFullPath(_contentDirectory);

            EnsureDirectory(_contentDirectory);
        }

        public async Task<Snapshot> Provision(Snapshot snapshot, CancellationToken cancellationToken)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            if (snapshot.Status != SnapshotStatus.Provisioning)
            {
                return snapshot;
            }

            try
            {
                IEnumerable<KeyValue> items = await GetContentAsync(snapshot, cancellationToken);
                await WriteContentFileAsync(snapshot, items, cancellationToken);
                snapshot.Status = SnapshotStatus.Ready;
                snapshot.StatusCode = 200;
                snapshot.LastModified = DateTimeOffset.UtcNow;
            }
            catch
            {
                snapshot.Status = SnapshotStatus.Failed;
                snapshot.StatusCode = 500;
                snapshot.LastModified = DateTimeOffset.UtcNow;
            }

            return snapshot;
        }

        private async Task<IEnumerable<KeyValue>> GetContentAsync(Snapshot snapshot, CancellationToken cancellationToken)
        {
            var result = new List<KeyValue>();
            if (snapshot.Filters == null) return result;

            foreach (var f in snapshot.Filters)
            {
                var keyFilter = new StringFilter { EqualsTo = f.Key, IsNull = f.Key == null };
                var labelFilter = new StringFilter { EqualsTo = f.Label, IsNull = f.Label == null };
                string continuation = null;
                do
                {
                    var page = await _keyValueProvider.QueryKeyValues(
                        new KeyValueSearchOptions
                        {
                            KeyFilter = keyFilter,
                            LabelFilter = labelFilter,
                            ContinuationToken = continuation
                        },
                        cancellationToken);

                    result.AddRange(page);
                    continuation = page.ContinuationToken;
                }
                while (!string.IsNullOrEmpty(continuation));
            }

            return result.GroupBy(k => (k.Key, k.Label)).Select(g => g.First());
        }

        private async Task WriteContentFileAsync(Snapshot snapshot, IEnumerable<KeyValue> items, CancellationToken cancellationToken)
        {
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

        private string GetContentFilePath(string snapshotId) => Path.Combine(_contentDirectory, snapshotId + ".ndjson");

        private static async Task<long> CountLines(string filePath, CancellationToken cancellationToken)
        {
            long count = 0;
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new StreamReader(fs);
            while (!reader.EndOfStream)
            {
                await reader.ReadLineAsync();
                count++;
                cancellationToken.ThrowIfCancellationRequested();
            }

            return count;
        }

        private static byte[] ComputeSha256(string filePath)
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var sha = SHA256.Create();
            return sha.ComputeHash(fs);
        }

        private static void ReplaceFile(string tempFilePath, string targetFilePath)
        {
            if (File.Exists(targetFilePath))
            {
                string bakFilePath = targetFilePath + ".bac";
                File.Replace(tempFilePath, targetFilePath, bakFilePath);
                if (File.Exists(bakFilePath))
                {
                    File.Delete(bakFilePath);
                }
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

        private static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}
