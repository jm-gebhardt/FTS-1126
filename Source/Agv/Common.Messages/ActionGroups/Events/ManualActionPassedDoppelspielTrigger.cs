using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Messages.ActionGroups.Events;

/// <summary>
/// Represents the event raised when a manual pick or drop passed the Doppelspieltrigger.
/// </summary>
/// <param name="Sap">The target input.</param>
/// <param name="TargetNodeName">The target station.</param>
/// <param name="IsPicking">True, if it is a pick action.</param>
/// <param name="HandlingUnit">The handling unit.</param>
/// <param name="CorrelationId">The correlation id of the action.</param>
/// <param name="AgvCorrelationId">The correlation id of the agv.</param>
[UsedImplicitly]
public record ManualActionPassedDoppelspielTrigger(string Sap, string TargetNodeName, bool IsPicking, string HandlingUnit, Guid CorrelationId, Guid AgvCorrelationId);