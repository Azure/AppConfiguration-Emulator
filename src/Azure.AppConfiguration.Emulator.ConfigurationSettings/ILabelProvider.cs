using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.ConfigurationSettings
{
    public interface ILabelProvider
    {
        ValueTask<IEnumerable<Label>> Get(
            LabelSearchOptions options,
            CancellationToken cancellationToken);
    }
}
