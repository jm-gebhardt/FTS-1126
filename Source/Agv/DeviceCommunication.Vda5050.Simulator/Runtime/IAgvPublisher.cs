using DeviceCommunication.Vda5050.Simulator.Domain;

namespace DeviceCommunication.Vda5050.Simulator.Runtime;

/// <summary>
/// Outgoing side of the simulator. Implementations must not block the caller.
/// </summary>
public interface IAgvPublisher
{
    void PublishConnection(AgvIdentity id, ConnectionState state, uint headerId, DateTimeOffset timestamp);

    void PublishState(AgvIdentity id, AgvStateSnapshot snapshot);

    void PublishFactsheet(AgvIdentity id, uint headerId, DateTimeOffset timestamp, string factsheetJson);
}
