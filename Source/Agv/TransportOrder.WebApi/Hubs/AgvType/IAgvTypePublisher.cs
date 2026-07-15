using Gebhardt.Acc.TransportOrder.WebApi.Dtos.AgvType;

namespace Gebhardt.Acc.TransportOrder.WebApi.Hubs.AgvType;

/// <summary>
/// Publishes agv type related events.
/// </summary>
public interface IAgvTypePublisher
{
    /// <summary>
    /// Publishes agv type acknowledged to all clients.
    /// </summary>
    Task SendAgvTypeAcknowledged(Guid id);

    /// <summary>
    /// Publishes a newly created agv type to all clients.
    /// </summary>
    Task SendAgvTypeCreated(AgvTypeDto agvType);

    /// <summary>
    /// Publishes agv type updated to all clients.
    /// </summary>
    /// <param name="agvType">The agv type.</param>
    Task SendAgvTypeUpdated(AgvTypeDto agvType);
}