using Gebhardt.Acc.Vda5050Simulator.Domain;

namespace Gebhardt.Acc.Vda5050Simulator.Runtime;

/// <summary>
/// Outgoing side of the simulator. Implementations must not block the caller.
/// </summary>
public interface IAgvPublisher
{
    void PublishConnection(AgvId id, ConnectionState state, uint headerId, DateTimeOffset timestamp);

    void PublishState(AgvId id, AgvStateSnapshot snapshot);

    void PublishFactsheet(AgvId id, uint headerId, DateTimeOffset timestamp);
}
