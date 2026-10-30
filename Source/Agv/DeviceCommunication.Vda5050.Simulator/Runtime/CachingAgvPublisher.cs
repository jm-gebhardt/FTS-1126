using DeviceCommunication.Vda5050.Simulator.Domain;

namespace DeviceCommunication.Vda5050.Simulator.Runtime;

/// <summary>
/// Stores every published state in the cache and forwards everything to the inner publisher.
/// </summary>
public sealed class CachingAgvPublisher(IAgvPublisher inner, IAgvStateCache cache) : IAgvPublisher
{
    public void PublishConnection(AgvIdentity id, ConnectionState state, uint headerId, DateTimeOffset timestamp)
        => inner.PublishConnection(id, state, headerId, timestamp);

    public void PublishState(AgvIdentity id, AgvStateSnapshot snapshot)
    {
        cache.Set(id, snapshot);
        inner.PublishState(id, snapshot);
    }

    public void PublishFactsheet(AgvIdentity id, uint headerId, DateTimeOffset timestamp, string factsheetJson)
        => inner.PublishFactsheet(id, headerId, timestamp, factsheetJson);
}
