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
}