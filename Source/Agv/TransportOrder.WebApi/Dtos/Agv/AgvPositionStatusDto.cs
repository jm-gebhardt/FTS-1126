using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;

/// <summary>
/// Model for the status of the AGV position
/// </summary>
/// <param name="CorrelationId">The correlationId of the AGV</param>
/// <param name="AgvId">Identifier of the AGV</param>
/// <param name="AgvPosition">AGV position</param>
[PublicAPI]
public record AgvPositionStatusDto(Guid CorrelationId, string AgvId, AgvPositionDto AgvPosition);