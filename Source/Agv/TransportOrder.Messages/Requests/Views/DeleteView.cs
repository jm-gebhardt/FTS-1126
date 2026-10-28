using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Views;

/// <summary>Represents a command to delete a view. Deleting a generated view does nothing.</summary>
/// <param name="CorrelationId">The id of the view.</param>
public record DeleteView(Guid CorrelationId) : CorrelatedBy<Guid>;