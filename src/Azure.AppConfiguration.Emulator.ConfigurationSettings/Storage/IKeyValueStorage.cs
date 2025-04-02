using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.ConfigurationSettings
{
    public interface IKeyValueStorage
    {
        Task AddKeyValue(KeyValue keyValue, CancellationToken cancellationToken);

        IAsyncEnumerable<KeyValue> QueryKeyValues();
    }
}
