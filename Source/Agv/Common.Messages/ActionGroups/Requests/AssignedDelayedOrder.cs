
namespace Gebhardt.Acc.Common.Messages.ActionGroups.Requests;

/// <summary>
/// Represents the response to the <see cref="TryAssignDelayedOrderToAgv"/> request.
/// </summary>
/// <param name="CorrelationId">The correlation id of the order, if any.</param>
public record AssignedDelayedOrder(Guid? CorrelationId);