using Gebhardt.Acc.Common.WebApi.Hubs;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.NewGraph;

namespace Gebhardt.Acc.TransportOrder.WebApi.Hubs.Graph;

/// <inheritdoc/>
public class GraphPublisher(IHubBroadcaster<GraphHub, IGraphClient> hubBroadcaster) : IGraphPublisher
{
    /// <inheritdoc/>
    public Task SendGraphChanged()
    {
        hubBroadcaster.Broadcast(c => c.GraphChanged());
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SendViewChanged(ViewDto view)
    {
        hubBroadcaster.Broadcast(c => c.ViewChanged(view));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SendViewDeleted(Guid viewId)
    {
        hubBroadcaster.Broadcast(c => c.ViewDeleted(viewId));
        return Task.CompletedTask;
    }
}