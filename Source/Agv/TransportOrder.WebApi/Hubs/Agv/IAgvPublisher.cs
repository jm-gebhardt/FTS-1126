using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;
using AgvPositionStatus = Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent.AgvPositionStatus;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Agv;

/// <summary>
/// Publishes Agv related Dtos
/// </summary>
public interface IAgvPublisher
{
    /// <summary>
    /// Publishes the status of an agv to all clients
    /// </summary>
    Task SendAgvStateAsync(Messages.MessageContent.Agv agv, bool isInitialState);

    /// <summary>
    /// Publishes the position of an agv to all clients
    /// </summary>
    Task SendAgvPositionAsync(AgvPositionStatus agvPosition);

    /// <summary>
    /// Publishes the status of an agv route to all clients
    /// </summary>
    /// <param name="agvRouteReadModel"></param>
    /// <returns></returns>
    Task SendAgvRouteAsync(AgvRouteReadModel agvRouteReadModel);
}