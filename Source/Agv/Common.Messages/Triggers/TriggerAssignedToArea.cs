using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Messages.Triggers;

/// <summary>
/// Represents the event raised when a trigger has been assigned to an area (polygon).
/// </summary>
/// <param name="TriggerId">The id of the trigger.</param>
/// <param name="TriggerName">The trigger name.</param>
/// <param name="AreaId">The id for the area (polygon location).</param>
[PublicAPI]
public record TriggerAssignedToArea(Guid TriggerId, string TriggerName, Guid AreaId);