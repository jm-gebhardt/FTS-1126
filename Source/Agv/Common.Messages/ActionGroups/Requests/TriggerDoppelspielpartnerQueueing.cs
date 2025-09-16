using Gebhardt.Acc.Common.Domain.Models.Graphs;
using MassTransit;

namespace Gebhardt.Acc.Common.Messages.ActionGroups.Requests;

/// <summary>
/// Provides a request to trigger queueing of a pick Doppelspiel partner, if available.
/// </summary>
/// <param name="CorrelationId">The correlation id of the action group.</param>
/// <param name="AssignedAgv">The assigned agv.</param>
/// <param name="TargetNode">The target node of the drop.</param>
public record TriggerDoppelspielpartnerQueueing(Guid CorrelationId, Guid AssignedAgv, GraphNode TargetNode): CorrelatedBy<Guid>;