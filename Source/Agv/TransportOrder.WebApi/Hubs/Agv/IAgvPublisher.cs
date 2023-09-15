using TransportOrder.Messages.Requests.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Agv;

/// <summary>
/// Publishes Agv related Dtos
/// </summary>
public interface IAgvPublisher
{
    /// <summary>
    /// Publishes the status of an agv to all clients
    /// </summary>
    public Task SendAgvStateAsync(global::TransportOrder.Messages.MessageContent.Agv agv, bool isInitialState);

    /// <summary>
    /// Publishes the position of an agv to all clients
    /// </summary>
    public Task SendAgvPositionAsync(AgvPositionStatus agvPosition);
}