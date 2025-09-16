using Gebhardt.Acc.Common.Domain.Models.Graphs;
using MassTransit;

namespace Gebhardt.Acc.Common.Messages.AgvManagement.Commands;

/// <summary>
/// Provides a command to occupy a station.
/// </summary>
/// <param name="Station">The station name.</param>
/// <param name="AgvCorrelationId">The correlation id of the agv.</param>
public record OccupyStation(GraphNode Station, Guid AgvCorrelationId) : CorrelatedBy<Guid>
{
    /// <inheritdoc cref="CorrelatedBy{Key}"/>>
    public Guid CorrelationId => Station.Id;
}