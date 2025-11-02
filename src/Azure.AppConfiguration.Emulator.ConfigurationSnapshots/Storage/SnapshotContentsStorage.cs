// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.ConfigurationSnapshots
{
    /// <summary>
    /// Stores snapshot content files (simulating blob storage). CreateContent writes provided key-values to a file.
    /// </summary>
    public sealed class SnapshotContentsStorage : ISnapshotContentsStorage
    {
        private readonly SnapshotsStorageOptions _options;
        private readonly string _contentDirectory;

        public SnapshotContentsStorage(
            IOptions<SnapshotsStorageOptions> options,
            IHostingEnvironment host)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            _options = options.Value ?? throw new ArgumentNullException(nameof(options));

            string directory = _options.ContentDirectory;
            if (!Path.IsPathRooted(directory))
            {
                directory = Path.Combine(host.ContentRootPath, directory);
            }

            directory = Path.GetFullPath(directory);
            _contentDirectory = directory;

            EnsureDirectory(_contentDirectory);
        }

        public async Task<MediaInfo> CreateContent(string fileName, IEnumerable<KeyValue> items, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            // Build full path inside content directory
            string targetFilePath = Path.Combine(_contentDirectory, fileName);
            string tempFilePath = targetFilePath + ".tmp";

            MediaInfo media = new MediaInfo
            {
                Category = "snapshots",
                ContentType = "application/x-ndjson"
            };

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
                throw new TimeoutException("ProvisionSnapshotContent", ex);
            }

            ReplaceFile(tempFilePath, targetFilePath);

            long lineCount = await CountLines(targetFilePath, cancellationToken);
            long fileSizeBytes = new FileInfo(targetFilePath).Length;

            media.Name = Path.GetFileName(targetFilePath);
            media.Size = fileSizeBytes; // bytes of file content
            media.Etag = SnapshotHelper.GenerateEtag();
            media.Sha256Hash = ComputeSha256(targetFilePath);

            return media;
        }

        /// <summary>
        /// Stream content items described by MediaInfo starting at a line offset.
        /// </summary>
        public async IAsyncEnumerable<KeyValue> GetContent(MediaInfo media, long offset, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (media == null)
            {
                throw new ArgumentNullException(nameof(media));
            }

            if (string.IsNullOrEmpty(media.Name))
            {
                yield break;
            }

            string filePath = Path.Combine(_contentDirectory, media.Name);
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
