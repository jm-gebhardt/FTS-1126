using JetBrains.Annotations;

namespace Gebhardt.Acc.KaeserSidecar.Messages;

/// <summary>
/// Represents the event raised when a manual pick or drop passed the Doppelspieltrigger.
/// </summary>
/// <param name="Sap">The target input.</param>
/// <param name="TargetNode">The target station.</param>
/// <param name="IsPicking">True, if it is a pick action.</param>
/// <param name="HandlingUnit">The handling unit.</param>
/// <param name="CorrelationId">The correlation id of the action.</param>
/// <param name="AgvCorrelationId">The correlation id of the agv.</param>
[UsedImplicitly]
public record ManualActionPassedDoppelspielTrigger(string Sap, string TargetNode, bool IsPicking, string HandlingUnit, Guid CorrelationId, Guid AgvCorrelationId);