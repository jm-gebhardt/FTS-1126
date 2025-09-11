using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.ActionGroup;

/// <summary>
/// Provides a request to trigger completion of an action.
/// </summary>
/// <param name="ActionGroupCorrelationId">The correlation id of the action group.</param>
/// <param name="ActionCorrelationId">The correlation id of the action.</param>
/// <param name="User">The user who completed the action group.</param>
[UsedImplicitly]
public record TriggerActionCompletion(Guid ActionGroupCorrelationId, Guid ActionCorrelationId, string User);