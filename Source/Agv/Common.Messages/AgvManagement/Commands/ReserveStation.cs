using Gebhardt.Acc.Common.Domain.Models.Graphs;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.Common.Messages.AgvManagement.Commands;

/// <summary>
/// Provides a command to reserve a (AKL) station.
/// </summary>
/// <param name="Station">The station name.</param>
/// <param name="AgvCorrelationId">The correlation id of the agv.</param>
[PublicAPI]
public record ReserveStation(GraphNode Station, Guid AgvCorrelationId) : CorrelatedBy<Guid>
{
    /// <inheritdoc cref="CorrelatedBy{Key}"/>>
    public Guid CorrelationId => Station.Id;
}