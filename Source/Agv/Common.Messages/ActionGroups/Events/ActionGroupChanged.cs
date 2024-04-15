using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;

/// <summary>
/// An event reporting that an action group has changed
/// </summary>
/// <param name="ActionGroup">Action group</param>
/// <param name="CorrelationId">The correlationId of the action group</param>
[PublicAPI]
public record ActionGroupChanged(ActionGroup ActionGroup, Guid CorrelationId) : CorrelatedBy<Guid>;