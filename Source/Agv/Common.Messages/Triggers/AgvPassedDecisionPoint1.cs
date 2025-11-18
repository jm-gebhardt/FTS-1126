using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Messages.Triggers;

/// <summary>
/// Represents the event raised when an agv passed decision point 1.
/// </summary>
/// <param name="AgvData">The agv data.</param>
/// <param name="AgvStatus">The agv status.</param>
/// <param name="ActionGroup">The current action group.</param>
/// <param name="Cargo">The current cargo.</param>
/// <param name="Errors">The errors of the agv.</param>
/// <param name="ConsideredStations">The stations to consider for target selection.</param>
/// <param name="UseJamState">True, if the jam state should be considered during target selection.</param>
[PublicAPI]
public record AgvPassedDecisionPoint1(AgvData AgvData, AgvStatus AgvStatus, ActionGroup? ActionGroup, ICollection<Cargo> Cargo, ICollection<AgvError> Errors, ICollection<Guid> ConsideredStations, bool UseJamState);