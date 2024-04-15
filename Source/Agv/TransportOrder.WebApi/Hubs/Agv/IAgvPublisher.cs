using Gebhardt.StoreWare.Agv.Common.Domain.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Agv;

/// <summary>
/// Publishes Agv related events.
/// </summary>
public interface IAgvPublisher
{
    /// <summary>
    /// Publishes the status of an agv to all clients.
    /// </summary>
    Task SendAgvStateChangedAsync(Messages.MessageContent.Agv agv);

    /// <summary>
    /// Publishes that an agv has been discovered to all clients.
    /// </summary>
    /// <param name="agv">The agv.</param>
    Task SendAgvDiscoveredAsync(Messages.MessageContent.Agv agv);

    /// <summary>
    /// Publishes the position of an Agv to all clients.
    /// </summary>
    Task SendAgvPositionAsync(AgvPositionStatus agvPosition);

    /// <summary>
    /// Publishes the Agv locked changed event to all clients.
    /// </summary>
    /// <param name="correlationId">The correlationId of the agv.</param>
    /// <param name="isLocked">True if the agv is locked.</param>
    Task SendAgvLockedChangedAsync(Guid correlationId, bool isLocked);

    /// <summary>
    /// Publishes the Agv online changed event to all clients.
    /// </summary>
    /// <param name="correlationId">The correlationId of the agv.</param>
    /// <param name="isOnline">True if the agv is online.</param>
    Task SendAgvOnlineChangedAsync(Guid correlationId, bool isOnline);

    /// <summary>
    /// Publishes the Agv error changed event to all clients.
    /// </summary>
    /// <param name="correlationId">The correlationId of the agv</param>
    /// <param name="errors">The errors of the agv.</param>
    Task SendAgvErrorChangedAsync(Guid correlationId, List<AgvError> errors);

    /// <summary>
    /// Publishes the Agv removed event to all clients.
    /// </summary>
    /// <param name="correlationId">The correlationId of the agv</param>
    Task SendAgvRemovedAsync(Guid correlationId);

    /// <summary>
    /// Publishes the status of an Agv route to all clients.
    /// </summary>
    /// <param name="currentAgvRouteReadModel">The current route of the agv.</param>
    Task SendAgvRouteChangedAsync(CurrentAgvRouteReadModel currentAgvRouteReadModel);

    /// <summary>
    /// Publishes the currently assigned action group of an Agv to all clients.
    /// </summary>
    /// <param name="correlationId">The correlationId of the agv</param>
    /// <param name="assignedActionGroupCorrelationId">Can be an action group correlationId or null (if the last action group has completed).</param>
    Task SendAssignedActionGroupChangedAsync(Guid correlationId, Guid? assignedActionGroupCorrelationId);

    /// <summary>
    /// Publishes the battery status of an Agv to all clients.
    /// </summary>
    /// <param name="batteryChargeChanged">The battery changed object.</param>
    Task SendAgvBatteryStatusAsync(AgvBatteryChargeChanged batteryChargeChanged);

    /// <summary>
    /// Publishes the cargo of an Agv to all clients.
    /// </summary>
    /// <param name="cargos">The cargos of the agv.</param>
    Task SendCargoChangeAsync(IReadOnlyCollection<Cargo> cargos);

    /// <summary>
    /// Publishes the hasCargo state change of an Agv to all clients.
    /// </summary>
    /// <param name="correlationId">The correlationId of the agv.</param>
    /// <param name="hasCargo">True if the agv is loaded with cargo.</param>
    Task SendAgvHasCargoChangedAsync(Guid correlationId, bool hasCargo);
}