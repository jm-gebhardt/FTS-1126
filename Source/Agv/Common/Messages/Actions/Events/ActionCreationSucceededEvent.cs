using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.Actions.Events;

/// <summary>
/// Sent once the ActionStateMachine was successfully created
/// </summary>
/// <param name="CorrelationId"></param>
[JetBrains.Annotations.PublicAPI]
public record ActionCreationSucceededEvent(Guid CorrelationId) : CorrelatedBy<Guid>;
