using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Views;

/// <summary>
/// Request for the PNG background image of a view.
/// </summary>
/// <param name="CorrelationId">The id of the view.</param>
public record GetViewBackgroundImage(Guid CorrelationId) : CorrelatedBy<Guid>;