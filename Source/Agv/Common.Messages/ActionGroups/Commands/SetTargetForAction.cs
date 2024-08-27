using MassTransit;

namespace Gebhardt.Acc.Common.Messages.ActionGroups.Commands;

/// <summary>
/// Provides a command to set the target of an incomplete action.
/// </summary>
/// <param name="ActionId">The action id.</param>
/// <param name="ActionGroupId">The action group id.</param>
/// <param name="AssignedAgv">The correlation id of the assigned agv (if available).</param>
/// <param name="TargetNodeId">The node id of the chosen target node.</param>
/// <param name="ShouldNotPickNext">The agv should not pick next.</param>
public record SetTargetForAction(Guid ActionId, Guid ActionGroupId, Guid? AssignedAgv, string TargetNodeId, bool ShouldNotPickNext) : CorrelatedBy<Guid>
{
    /// <inheritdoc cref="CorrelatedBy{TKey}"/>>
    public Guid CorrelationId => ActionGroupId;

    /// <summary>
    /// Deconstruct <see cref="SetTargetForAction"/>.
    /// </summary>
    public void Deconstruct(out Guid actionId, out Guid actionGroupId, out Guid? assignedAgv, out string targetNodeId, out bool shouldNotPickNext)
    {
        assignedAgv = AssignedAgv;
        actionId = ActionId;
        actionGroupId = ActionGroupId;
        targetNodeId = TargetNodeId;
        shouldNotPickNext = ShouldNotPickNext;
    }
}