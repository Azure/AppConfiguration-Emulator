using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.ConfigurationSnapshots
{
    public interface ISnapshotsStorage
    {
        IAsyncEnumerable<Snapshot> QuerySnapshots();

        Task<Snapshot> GetSnapshot(string snapshotId, CancellationToken cancellationToken);

        Task AddSnapshot(Snapshot snapshot, CancellationToken cancellationToken);

        Task UpdateSnapshot(Snapshot snapshot, CancellationToken cancellationToken);

        IAsyncEnumerable<KeyValue> ReadSnapshotContent(Snapshot snapshot, long offset);
    }
}
