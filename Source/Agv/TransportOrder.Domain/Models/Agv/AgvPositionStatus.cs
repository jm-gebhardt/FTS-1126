using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;

/// <summary>
/// Model for the status of the AGV position
/// </summary>
/// <param name="CorrelationId">The correlationId of the new AGV position</param>
/// <param name="AgvId">Identifier of the AGV</param>
/// <param name="AgvPosition">AGV position</param>
[PublicAPI]
public record AgvPositionStatus(Guid CorrelationId, string AgvId, AgvPosition AgvPosition);