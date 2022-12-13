using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.Common.Messages.Actions.Events;

/// <summary>
/// Sent as soon as the action has been successfully executed by the Agv
/// </summary>
[PublicAPI]
public record ActionStatusChangedEvent(Guid CorrelationId, ActionStatusChangedType ChangedType);