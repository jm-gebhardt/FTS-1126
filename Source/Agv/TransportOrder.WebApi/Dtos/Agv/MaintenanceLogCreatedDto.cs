using Gebhardt.Acc.Common.Messages;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;

/// <summary>
/// Provides a dto for the maintenance log created event.
/// </summary>
/// <param name="MaintenanceLog">The maintenance log.</param>
/// <param name="AgvCorrelationId">The agv correlation id.</param>
[PublicAPI]
public record MaintenanceLogCreatedDto(AgvMaintenanceLogDto MaintenanceLog, Guid AgvCorrelationId): DatedMessage;