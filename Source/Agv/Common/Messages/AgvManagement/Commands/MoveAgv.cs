using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;

/// <summary>
/// Event sent when Agv is requested to move to a specific destination node
/// <param name="CorrelationId">AgvCorrelationId which should move</param>
/// <param name="TargetNodeId">The target node to move to</param>
/// </summary>
public record MoveAgv(Guid CorrelationId, string TargetNodeId) : CorrelatedBy<Guid>;
