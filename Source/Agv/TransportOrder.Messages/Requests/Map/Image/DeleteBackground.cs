using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Map.Image;

/// <summary>Command to delete any custom background and revert to the default.</summary>
/// <param name="CorrelationId">The correlation id of the map.</param>
public record DeleteBackground(Guid CorrelationId) : CorrelatedBy<Guid>;