using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Command to mark the AgvGraph existing in the Agv.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
/// <param name="AgvTypeIdentifier">The identifier of the AGV type this graph belongs to, or null if unknown.</param>
public record SetAgvGraphExist(Guid CorrelationId, string? AgvTypeIdentifier) : CorrelatedBy<Guid>;