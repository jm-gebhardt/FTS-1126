using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Views;

/// <summary>
/// Request for one stored or generated view.
/// </summary>
/// <param name="CorrelationId">The id of the view.</param>
public record GetView(Guid CorrelationId) : CorrelatedBy<Guid>;