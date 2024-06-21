using MassTransit;

namespace Gebhardt.Acc.PrismatCommunication.Messages.Commands;

/// <summary>
/// Provides a command to store passing of a trigger point.
/// </summary>
/// <param name="CorrelationId">The id of the host order.</param>
/// <param name="AssignedAgv">The agv assigned to the order.</param>
/// <param name="TargetNodeId">The decided target node id.</param>
public record PassDecisionPoint(Guid CorrelationId, Guid? AssignedAgv, string TargetNodeId) : CorrelatedBy<Guid>;