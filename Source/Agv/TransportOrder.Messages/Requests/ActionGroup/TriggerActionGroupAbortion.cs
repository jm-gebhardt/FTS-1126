using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.ActionGroup;

/// <summary>
/// Provides a command to trigger abortion of an action group.
/// </summary>
/// <param name="CorrelationId">The correlation id of the action group.</param>
/// <param name="AbortReason">The reason for the abort.</param>
/// <param name="User">The user that triggered the abortion.</param>
public record TriggerActionGroupAbortion(Guid CorrelationId, AbortReason AbortReason, string User);