// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.ConfigurationSettings
{
    public class KeyValueProvider :
        IKeyValueProvider,
        IKeyProvider,
        ILabelProvider
    {
        private readonly IKeyValueStorage _storage;

        public KeyValueProvider(IKeyValueStorage storage)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public ValueTask<Page<KeyValue>> Get(
            KeyValueSearchOptions options,
            CancellationToken cancellationToken)
        {
            //
            // TODO: Implement this method
            //

            return new ValueTask<Page<KeyValue>>(
                new Page<KeyValue>(
                    new[]
                    {
                        new KeyValue
                        {
                            Key = "k1",
                            Label = "prod",
                            Value = "Lorem ipsum dolor sit amet, consectetur adipiscing elit",
                            Etag = "123456",
                            Created = DateTimeOffset.UtcNow,
                            Locked = false,
                            Tags = new Dictionary<string, string>
                            {
                                { "tag1", "value1" },
                                { "tag2", "value2" }
                            }
                        },
                        new KeyValue
                        {
                            Key = "k2",
                            Label = "dev",
                            Value = "Hello World!",
                            Etag = "000000",
                            Created = DateTimeOffset.UtcNow.AddMinutes(-234),
                            Locked = false
                        }
                    })
                {
                    Etag = "--<etag>--",
                    ContinuationToken = "<continuation-token>",
                    TotalItemsCount = 2
                });
        }

        public ValueTask<KeyValue> Get(
            string key,
            string label,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public ValueTask<IEnumerable<Key>> Get(
            KeySearchOptions options,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public ValueTask<IEnumerable<Label>> Get(
            LabelSearchOptions options,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public ValueTask Remove(
            KeyValue kv,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public ValueTask Set(
            KeyValue kv,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public ValueTask Lock(
           KeyValue kv,
           CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public ValueTask Unlock(
            KeyValue kv,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
