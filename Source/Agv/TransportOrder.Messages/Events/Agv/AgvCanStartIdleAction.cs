using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.Agv;

/// <summary>
/// Represents an event that is raised when an agv has no other actions to perform.
/// </summary>
/// <param name="CorrelationId">The agv correlation id.</param>
public record AgvCanStartIdleAction(Guid CorrelationId) : CorrelatedBy<Guid>;