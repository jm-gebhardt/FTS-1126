using Gebhardt.Acc.Common.Domain.Models.Graphs;
using MassTransit;

namespace Gebhardt.Acc.Common.Messages.Graphs.Events;

/// <summary>
/// Represents the event raised when a JamState request for a station was sent.
/// </summary>
/// <param name="StationNode">The id of the station.</param>
public record JamStateRequested(GraphNode StationNode) : CorrelatedBy<Guid>
{
    /// <inheritdoc cref="CorrelatedBy{TKey}"/>
    public Guid CorrelationId => StationNode.Id;
}