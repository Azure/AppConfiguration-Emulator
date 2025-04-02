using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.ConfigurationSettings
{
    public interface IKeyProvider
    {
        ValueTask<IEnumerable<Key>> Get(
            KeySearchOptions options,
            CancellationToken cancellationToken);
    }
}
