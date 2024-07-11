using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Messages.Triggers;

/// <summary>
/// Represents the event raised when a trigger has been assigned to a node.
/// </summary>
/// <param name="TriggerId">The id of the trigger.</param>
/// <param name="TriggerName">The trigger name.</param>
/// <param name="NodeId">The node id.</param>
[PublicAPI]
public record TriggerAssignedToNode(Guid TriggerId, string TriggerName, string NodeId);