using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv;

/// <summary>
/// Defines a result for the GetAgvPositions request
/// </summary>
public record GotAgvPositions(List<AgvPositionStatus>? AgvPositions);

/// <summary>
/// Represents the position status of an agv.
/// </summary>
/// <param name="CorrelationId">The correlationId of the AGV</param>
/// <param name="AgvId">Identifier of the AGV</param>
/// <param name="AgvPosition">AGV position</param>
[UsedImplicitly]
public record AgvPositionStatus(Guid CorrelationId, string AgvId, AgvPosition AgvPosition);