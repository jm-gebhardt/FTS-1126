using Gebhardt.Acc.Common.Messages.ActionGroups;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Represents the event raised when an Agv is requested to move to a specific destination node.
/// <param name="CorrelationId">AgvCorrelationId which should move.</param>
/// <param name="TargetNodeId">The target node to move to.</param>
/// <param name="Creator">The creator of the <see cref="ActionGroup"/>.</param>
/// </summary>
public record MoveAgv(Guid CorrelationId, string TargetNodeId, ActionGroupCreator Creator) : CorrelatedBy<Guid>;