using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Views;

/// <summary>Represents a command to delete the background image of a view.</summary>
/// <param name="CorrelationId">The id of the view.</param>
public record DeleteViewBackground(Guid CorrelationId) : CorrelatedBy<Guid>;