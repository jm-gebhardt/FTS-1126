using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.Views;

/// <summary>
/// Represents the event raised when a view was deleted.
/// </summary>
/// <param name="CorrelationId">The id of the deleted view.</param>
public record ViewDeleted(Guid CorrelationId) : CorrelatedBy<Guid>;