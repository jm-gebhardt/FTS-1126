using MassTransit;

namespace Gebhardt.Acc.Common.Messages.Graphs.Events;

/// <summary>
/// Represents the event raised when a station was unlocked.
/// </summary>
/// <param name="StationNodeId">The node id of the station.</param>
public record StationUnlocked(string StationNodeId) : CorrelatedBy<string>
{
    /// <inheritdoc cref="CorrelatedBy{TKey}"/>>
    public string CorrelationId => StationNodeId;
}