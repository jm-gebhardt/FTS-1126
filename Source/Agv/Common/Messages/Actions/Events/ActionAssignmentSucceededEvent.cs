using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.Actions.Events;

/// <summary>
/// Sent once the action was successfully assigned to an Agv in the ActionStateMachine
/// </summary>
/// <param name="CorrelationId"></param>
[JetBrains.Annotations.PublicAPI]
public record ActionAssignmentSucceededEvent(Guid CorrelationId) : CorrelatedBy<Guid>;