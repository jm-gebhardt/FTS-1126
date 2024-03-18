using Gebhardt.StoreWare.Agv.Common.WebApi;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;

/// <summary>
/// Model for the status of the agv position.
/// </summary>
/// <param name="CorrelationId">The correlationId of the agv.</param>
/// <param name="SerialNumber">The serial number of the agv.</param>
/// <param name="AgvPosition">The agv position.</param>
[PublicAPI]
public record AgvPositionStatusDto(Guid CorrelationId, string SerialNumber, AgvPositionDto AgvPosition) : DatedMessage;