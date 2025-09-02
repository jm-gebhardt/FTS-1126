namespace Gebhardt.Acc.TransportOrder.Messages.Requests.MaintenanceLog;

/// <summary>
/// Represents the response to the <see cref="GetMaintenanceLogsForAgv"/> request.
/// </summary>
/// <param name="MaintenanceLogs">The maintenance logs for the agv.</param>
public record GotMaintenanceLogs(ICollection<Domain.Models.MaintenanceLog.MaintenanceLog> MaintenanceLogs);