using Gebhardt.Acc.TransportOrder.WebApi.Dtos.NewGraph;

namespace Gebhardt.Acc.TransportOrder.WebApi.Hubs.Graph;

/// <summary>
/// Publishes graph and view related events.
/// </summary>
public interface IGraphPublisher
{
    /// <summary>
    /// Publishes to all clients that the graph has changed.
    /// </summary>
    Task SendGraphChanged();

    /// <summary>
    /// Publishes a created or changed view to all clients.
    /// </summary>
    /// <param name="view">The created or changed view.</param>
    Task SendViewChanged(ViewDto view);

    /// <summary>
    /// Publishes a deleted view to all clients.
    /// </summary>
    /// <param name="viewId">The id of the deleted view.</param>
    Task SendViewDeleted(Guid viewId);
}