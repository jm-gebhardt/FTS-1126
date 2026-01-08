using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Messages.Triggers;

/// <summary>
/// Represents the event raised when an agv passed the short loop waiting handler trigger.
/// </summary>
/// <param name="AgvData">The agv data.</param>
/// <param name="AgvStatus">The agv status.</param>
/// <param name="ActionGroup">The current action group.</param>
/// <param name="Cargo">The current cargo.</param>
/// <param name="Errors">The errors of the agv.</param>
/// <param name="PrimaryTarget">The primary target for the waiting point.</param>
/// <param name="SecondaryTargets">The optional secondary targets for the waiting point.</param>
[PublicAPI]
public record AgvPassedShortLoopWaitingHandler(AgvData AgvData, AgvStatus AgvStatus, ActionGroup? ActionGroup, ICollection<Cargo> Cargo, ICollection<AgvError> Errors, Guid PrimaryTarget=default, ICollection<Guid>? SecondaryTargets=null);