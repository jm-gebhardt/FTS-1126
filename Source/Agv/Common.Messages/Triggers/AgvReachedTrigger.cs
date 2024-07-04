using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;

namespace Gebhardt.Acc.Common.Messages.Triggers;

/// <summary>
/// Represents the event raised when an agv reached a trigger.
/// </summary>
/// <param name="TriggerId">The id of the trigger.</param>
/// <param name="AgvData">The agv data.</param>
/// <param name="AgvStatus">The agv status.</param>
/// <param name="ActionGroup">The current action group.</param>
/// <param name="Cargo">The current cargo.</param>
/// <param name="Errors">The errors of the agv.</param>
public record AgvReachedTrigger(Guid TriggerId, AgvData AgvData, AgvStatus AgvStatus, ActionGroup? ActionGroup, ICollection<Cargo> Cargo, ICollection<AgvError> Errors);