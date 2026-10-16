using Gebhardt.Acc.Vda5050Simulator.Domain;

namespace Gebhardt.Acc.Vda5050Simulator.Runtime;

/// <summary>
/// Stores every published state in the cache and forwards everything to the inner publisher.
/// </summary>
public sealed class CachingAgvPublisher(IAgvPublisher inner, IAgvStateCache cache) : IAgvPublisher
{
    public void PublishConnection(AgvId id, ConnectionState state, uint headerId, DateTimeOffset timestamp)
        => inner.PublishConnection(id, state, headerId, timestamp);

    public void PublishState(AgvId id, AgvStateSnapshot snapshot)
    {
        cache.Set(id, snapshot);
        inner.PublishState(id, snapshot);
    }

    public void PublishFactsheet(AgvId id, uint headerId, DateTimeOffset timestamp)
        => inner.PublishFactsheet(id, headerId, timestamp);
}
