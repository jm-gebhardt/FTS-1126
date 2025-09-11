using Gebhardt.Acc.Common.Messages;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;

/// <summary>
/// Provides a dto for an agv maintenance event.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
/// <param name="Maintenance">The last maintenance</param>
public record AgvMaintainedDto(Guid CorrelationId, MaintenanceDto Maintenance): DatedMessage;