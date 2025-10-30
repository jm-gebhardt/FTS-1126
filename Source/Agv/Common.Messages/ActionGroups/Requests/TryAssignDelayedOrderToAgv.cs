using Gebhardt.Acc.Common.Domain.Models.Graphs;
using MassTransit;

namespace Gebhardt.Acc.Common.Messages.ActionGroups.Requests;

/// <summary>
/// Provides a request to assign orders pick from the given station that have been delayed for the given agv to the given assignable agv.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv, that triggered the delay of the order.</param>
/// <param name="AssignableAgv">The correlation id of the agv to be assigned to the order.</param>
/// <param name="PickStation">The pick station of the delayed order.</param>
/// <param name="OrdersToIgnore">Order that should be ignored during assignment.</param>
public record TryAssignDelayedOrderToAgv(Guid CorrelationId, Guid AssignableAgv, GraphNode PickStation, ICollection<Guid>? OrdersToIgnore = null) : CorrelatedBy<Guid>;