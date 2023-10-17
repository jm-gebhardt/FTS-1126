using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;
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
    Task SendAgvStateChangedAsync(Messages.MessageContent.Agv agv);

    /// <summary>
    /// Publishes that an Agv has been discovered to all clients
    /// </summary>
    /// <param name="agv"></param>
    /// <returns></returns>
    Task SendAgvDiscoveredAsync(Messages.MessageContent.Agv agv);

    /// <summary>
    /// Publishes the position of an agv to all clients
    /// </summary>
    Task SendAgvPositionAsync(AgvPositionStatus agvPosition);

    /// <summary>
    /// Publishes the agv locked changed event to all clients
    /// </summary>
    /// <param name="correlationId">The CorrelationId of the Agv</param>
    /// <param name="isLocked"></param>
    /// <returns></returns>
    Task SendAgvLockedChangedAsync(Guid correlationId, bool isLocked);

    /// <summary>
    /// Publishes the agv online changed event to all clients
    /// </summary>
    /// <param name="correlationId">The CorrelationId of the Agv</param>
    /// <param name="isOnline"></param>
    /// <returns></returns>
    Task SendAgvOnlineChangedAsync(Guid correlationId, bool isOnline);

    /// <summary>
    /// Publishes the agv error changed event to all clients
    /// </summary>
    /// <param name="correlationId">The CorrelationId of the Agv</param>
    /// <param name="errors"></param>
    /// <returns></returns>
    Task SendAgvErrorChangedAsync(Guid correlationId, List<AgvError> errors);

    /// <summary>
    /// Publishes the agv removed event to all clients
    /// </summary>
    /// <param name="correlationId">The CorrelationId of the Agv</param>
    /// <returns></returns>
    Task SendAgvRemovedAsync(Guid correlationId);

    /// <summary>
    /// Publishes the status of an agv route to all clients
    /// </summary>
    /// <param name="agvRouteReadModel"></param>
    /// <returns></returns>
    Task SendAgvRouteAsync(AgvRouteReadModel agvRouteReadModel);

    /// <summary>
    /// Publishes the currently assigned action group of an agv to all clients
    /// </summary>
    /// <param name="correlationId">The CorrelationId of the Agv</param>
    /// <param name="assignedActionGroup">Can be an ActionGroup or null (if the last ActionGroup has completed)</param>
    /// <returns></returns>
    Task SendAssignedActionGroupChangedAsync(Guid correlationId, Common.Messages.ActionGroups.ActionGroup? assignedActionGroup);

    /// <summary>
    /// Publishes the battery status of an agv to all clients
    /// </summary>
    /// <param name="batteryChargeChanged"></param>
    /// <returns></returns>
    Task SendAgvBatteryStatus(AgvBatteryChargeChanged batteryChargeChanged);
}