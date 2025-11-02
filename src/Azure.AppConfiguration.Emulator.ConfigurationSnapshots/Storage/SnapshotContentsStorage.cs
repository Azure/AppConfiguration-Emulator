// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            string directory = _options.ContentDirectory;
            if (!Path.IsPathRooted(directory))
            {
                directory = Path.Combine(host.ContentRootPath, directory);
            }

            directory = Path.GetFullPath(directory);
            _contentDirectory = directory;

            EnsureDirectory(_contentDirectory);
        }

        public async Task<MediaInfo> Provision(Snapshot snapshot, CancellationToken cancellationToken)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            if (snapshot.Status != SnapshotStatus.Provisioning)
            {
                return snapshot.Media;
            }

            MediaInfo media = new MediaInfo();
            media.Category = "snapshots";
            media.ContentType = "application/x-ndjson";

            try
            {
                IEnumerable<KeyValue> items = await GetContentAsync(snapshot, cancellationToken);
                MediaInfo updated = await WriteContentFileAsync(snapshot, items, media, cancellationToken);
                snapshot.Status = SnapshotStatus.Ready;
                snapshot.StatusCode = 200;
                snapshot.LastModified = DateTimeOffset.UtcNow;
                snapshot.Media = updated;
            }
            catch
            {
                snapshot.Status = SnapshotStatus.Failed;
                snapshot.StatusCode = 500;
                snapshot.LastModified = DateTimeOffset.UtcNow;
            }

            return snapshot.Media;
        }

        public async IAsyncEnumerable<KeyValue> Get(
            Snapshot snapshot,
            long offset,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            if (snapshot.Status != SnapshotStatus.Ready && snapshot.Status != SnapshotStatus.Archived)
            {
                yield break;
            }

            if (snapshot.Media == null || string.IsNullOrEmpty(snapshot.Media.Name))
            {
                yield break;
            }

            string filePath = Path.Combine(_contentDirectory, snapshot.Media.Name);
            if (!File.Exists(filePath))
            {
                yield break;
            }

            using FileStream fs = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);

            NdJsonStreamReader<KeyValue> reader = new NdJsonStreamReader<KeyValue>(
                fs,
                (ref Utf8JsonReader r, out KeyValue kv) => r.TryReadKeyValue(out kv),
                _options.ReadBufferSizeHint,
                _options.MaxReadBufferSize);

            using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_options.ReadTimeout);

            long index = 0;
            await foreach (KeyValue kv in reader.ReadItems(cts.Token))
            {
                if (index++ < offset)
                {
                    continue;
                }

                yield return kv;
            }
        }

        private async Task<IEnumerable<KeyValue>> GetContentAsync(Snapshot snapshot, CancellationToken cancellationToken)
        {
            List<KeyValue> result = new List<KeyValue>();
            if (snapshot.Filters == null)
            {
                return result;
            }

            foreach (KeyValueFilter f in snapshot.Filters)
            {
                StringFilter keyFilter = new StringFilter { EqualsTo = f.Key, IsNull = f.Key == null };
                StringFilter labelFilter = new StringFilter { EqualsTo = f.Label, IsNull = f.Label == null };
                string continuation = null;
                do
                {
                    ConfigurationSettings.Page<KeyValue> page = await _keyValueProvider.QueryKeyValues(
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

        private async Task<MediaInfo> WriteContentFileAsync(
            Snapshot snapshot,
            IEnumerable<KeyValue> items,
            MediaInfo media,
            CancellationToken cancellationToken)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            string name = snapshot.Id + ".ndjson";
            string filePath = Path.Combine(_contentDirectory, name);
            string tempFilePath = filePath + ".tmp";

            try
            {
                using FileStream fs = new FileStream(
                    tempFilePath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    _options.WriteBufferSize);

                using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(_options.WriteTimeout);

                foreach (KeyValue kv in items)
                {
                    if (fs.Position > 0)
                    {
                        fs.WriteDelimiter();
                    }

                    using Utf8JsonWriter json = new Utf8JsonWriter(fs);
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

            FileInfo fi = new FileInfo(filePath);
            snapshot.Size = fi.Length;
            snapshot.ItemCount = await CountLines(filePath, cancellationToken);

            media.Name = name;
            media.Size = snapshot.ItemCount;
            media.Etag = SnapshotHelper.GenerateEtag();
            media.Sha256Hash = ComputeSha256(filePath);

            return media;
        }

        private static async Task<long> CountLines(string filePath, CancellationToken cancellationToken)
        {
            long count = 0;
            using FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using StreamReader reader = new StreamReader(fs);
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
            using FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using SHA256 sha = SHA256.Create();
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
