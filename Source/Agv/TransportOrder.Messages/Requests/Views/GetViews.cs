using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Views;

/// <summary>
/// Request for all views.
/// </summary>
/// <param name="CorrelationId">The identifier of this message.</param>
public record GetViews(Guid CorrelationId) : CorrelatedBy<Guid>;