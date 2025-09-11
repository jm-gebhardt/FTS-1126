using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.ActionGroup;

/// <summary>
/// Provides a request to trigger queueing of an action group.
/// </summary>
/// <param name="CorrelationId">The correlation id of the action group.</param>
/// <param name="User">The user who triggered queueing of the action group.</param>
[PublicAPI]
public record TriggerActionGroupQueueing(Guid CorrelationId, string User) : CorrelatedBy<Guid>;