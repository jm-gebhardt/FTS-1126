using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;

/// <summary>
/// Provides a command to force queueing of an action group.
/// </summary>
/// <param name="CorrelationId">The correlation id of the action group.</param>
[PublicAPI]
public record ForceQueueActionGroup(Guid CorrelationId) : CorrelatedBy<Guid>;