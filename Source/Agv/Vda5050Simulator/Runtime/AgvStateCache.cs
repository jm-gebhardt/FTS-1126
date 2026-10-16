using System.Collections.Concurrent;
using Gebhardt.Acc.Vda5050Simulator.Domain;

namespace Gebhardt.Acc.Vda5050Simulator.Runtime;

public sealed class AgvStateCache : IAgvStateCache
{
    private readonly ConcurrentDictionary<Guid, (AgvId Id, AgvStateSnapshot Snapshot)> _states = new();

    public void Set(AgvId id, AgvStateSnapshot snapshot)
    {
        _states.AddOrUpdate(
            id.CorrelationId,
            _ => (id, snapshot),
            (_, existing) => snapshot.HeaderId >= existing.Snapshot.HeaderId ? (id, snapshot) : existing);
    }

    public bool TryGet(Guid correlationId, out AgvId id, out AgvStateSnapshot snapshot)
    {
        if (_states.TryGetValue(correlationId, out var entry))
        {
            id = entry.Id;
            snapshot = entry.Snapshot;
            return true;
        }

        id = default;
        snapshot = null!;
        return false;
    }
}
