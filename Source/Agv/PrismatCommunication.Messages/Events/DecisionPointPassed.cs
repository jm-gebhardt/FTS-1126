namespace Gebhardt.Acc.PrismatCommunication.Messages.Events;

/// <summary>
/// Represents the event raise, when a host order passed the decision point.
/// </summary>
/// <param name="AssignedAgv">The assigned agv.</param>
/// <param name="TargetNodeId">The decided target node id.</param>
/// <param name="ShouldNotPickNext">The agv should not pick in the next step.</param>
public record DecisionPointPassed(Guid AssignedAgv, string TargetNodeId, bool ShouldNotPickNext);