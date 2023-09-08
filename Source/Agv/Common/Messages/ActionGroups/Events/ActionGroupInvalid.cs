using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;

/// <summary>
/// Represents an event reporting that an action group state is invalid.
/// </summary>
/// <param name="CorrelationId">The action group correlationId.</param>
/// <param name="ActionGroup">The action group that was invalid.</param>
/// <param name="ValidationErrors">The validation errors.</param>
public record ActionGroupInvalid(Guid CorrelationId, ActionGroup ActionGroup, [UsedImplicitly] ICollection<string> ValidationErrors) : CorrelatedBy<Guid>;
