using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.ConfigurationSettings
{
    public class KeyValueStorage : IKeyValueStorage
    {
        public IAsyncEnumerable<KeyValue> QueryKeyValues()
        {
            return AsyncEnumerable.Empty<KeyValue>();
        }

        public Task AddKeyValue(KeyValue kv, CancellationToken cancellationToken)
        {
            ValidateKeyValue(kv);

            //
            // TODO: Add implementation
            //

            return Task.CompletedTask;
        }

        private void ValidateKeyValue(KeyValue kv)
        {
            if (kv == null)
            {
                throw new ArgumentNullException(nameof(kv));
            }

            if (kv.Value != null && string.IsNullOrEmpty(kv.EncryptionKeyId))
            {
                throw new ArgumentNullException(nameof(kv.EncryptionKeyId));
            }
        }
    }
}
