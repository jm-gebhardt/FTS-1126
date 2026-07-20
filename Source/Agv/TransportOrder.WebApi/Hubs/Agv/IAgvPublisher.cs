using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.TransportOrder.Domain.Models.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Models.MaintenanceLog;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Messages.MessageContent;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;
using AgvPositionStatus = Gebhardt.Acc.TransportOrder.Messages.MessageContent.AgvPositionStatus;
using Cargo = Gebhardt.Acc.Common.Domain.Models.Cargo;

namespace Gebhardt.Acc.TransportOrder.WebApi.Hubs.Agv;

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
    /// Publishes the mode of an agv to all clients.
    /// </summary>
    Task SendAgvModeChangedAsync(Guid agvCorrelationId, AgvModeDto mode);

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
    /// <param name="batteryStatusChanged">The battery changed object.</param>
    Task SendAgvBatteryStatusAsync(AgvBatteryStatusChanged batteryStatusChanged);

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

    /// <summary>
    /// Publishes the new zones of an agv to all clients.
    /// </summary>
    /// <param name="correlationId">The correlationId of the agv.</param>
    /// <param name="zones">The new zones of the agv.</param>
    Task SendAgvZonesChangedAsync(Guid correlationId, ICollection<string> zones);

    /// <summary>
    /// Publishes the new device internally reserved stations of an agv to all clients.
    /// </summary>
    /// <param name="correlationId">The correlationId of the agv.</param>
    /// <param name="stations">The new stations of the agv.</param>
    Task SendAgvStationsChangedAsync(Guid correlationId, ICollection<string> stations);

    /// <summary>
    /// Publishes the operating data status of an Agv to all clients.
    /// </summary>
    /// <param name="operatingDataStatusChanged">The operating data changed object.</param>
    Task SendAgvOperatingDataStatusAsync(AgvOperatingDataStatusChanged operatingDataStatusChanged);

    /// <summary>
    /// Publishes the last maintenance of an agv to all clients.
    /// </summary>
    /// <param name="correlationId">The correlation id of the agv.</param>
    /// <param name="maintenance">The last maintenance.</param>
    /// <returns></returns>
    Task SendAgvMaintainedAsync(Guid correlationId, Maintenance maintenance);

    /// <summary>
    /// Publishes the last created maintenance log to all clients.
    /// </summary>
    /// <param name="correlationId">The correlation id of the agv.</param>
    /// <param name="maintenanceLog">The maintenance log.</param>
    /// <returns></returns>
    Task SendMaintenanceLogCreated(Guid correlationId, MaintenanceLog maintenanceLog);

    /// <summary>
    /// Publishes the last agv log to all clients.
    /// </summary>
    /// <param name="correlationId">The agv correlation id.</param>
    /// <param name="logEntry">The last agv log entry.</param>
    Task SendLogEntryCreated(Guid correlationId, LogEntry logEntry);

    /// <summary>
    /// Publishes the last battery log to all clients.
    /// </summary>
    /// <param name="correlationId">The agv correlation id.</param>
    /// <param name="logEntry">The last agv log entry.</param>
    Task SendBatteryLogEntryCreated(Guid correlationId, LogEntry logEntry);

    /// <summary>
    /// Publishes the agv operation mode change to all clients.
    /// </summary>
    /// <param name="agv">The agv.</param>
    Task SendOperationModeChanged(Messages.MessageContent.Agv agv);
}