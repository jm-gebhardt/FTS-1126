using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.Common.Messages.ActionGroups.Events;

/// <summary>
/// Represents an event raised, when an ActionGroup has been reassigned or reset.
/// </summary>
/// <param name="CorrelationId">The id of the ActionGroup.</param>
/// <param name="ActionGroupCreator">The creator of the action group.</param>
[PublicAPI]
public record ActionGroupReassignedOrReset(Guid CorrelationId, ActionGroupCreator ActionGroupCreator) : CorrelatedBy<Guid>;