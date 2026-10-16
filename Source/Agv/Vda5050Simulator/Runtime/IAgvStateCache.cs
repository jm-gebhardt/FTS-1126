using Gebhardt.Acc.Vda5050Simulator.Domain;

namespace Gebhardt.Acc.Vda5050Simulator.Runtime;

/// <summary>
/// Thread-safe cache of the latest published state per AGV.
/// </summary>
public interface IAgvStateCache
{
    /// <summary>
    /// Stores the state if it is newer than the cached one.
    /// </summary>
    void Set(AgvId id, AgvStateSnapshot snapshot);

    /// <summary>
    /// Gets the latest state of the AGV with the given correlation id.
    /// </summary>
    bool TryGet(Guid correlationId, out AgvId id, out AgvStateSnapshot snapshot);
}
