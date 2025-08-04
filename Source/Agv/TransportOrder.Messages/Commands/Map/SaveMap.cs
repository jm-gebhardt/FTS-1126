using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Map;

/// <summary>
/// Provides a command to save a map.
/// </summary>
/// <param name="Map">The map.</param>
public record SaveMap(Common.Domain.Models.Graphs.Map Map) : CorrelatedBy<Guid>
{
    /// <summary>
    /// Correlation‑Id of the map.
    /// </summary>
    public Guid CorrelationId => Map.CorrelationId;
}