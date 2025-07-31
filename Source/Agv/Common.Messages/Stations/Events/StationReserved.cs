using MassTransit;

namespace Gebhardt.Acc.Common.Messages.Stations.Events;

/// <summary>
/// Represents the event raised when a station has been reserved.
/// </summary>
/// <param name="StationName">The station name.</param>
/// <param name="AgvCorrelationId">The correlation id of the agv.</param>
public record StationReserved(string StationName, Guid AgvCorrelationId) : CorrelatedBy<string>
{
    /// <inheritdoc cref="CorrelatedBy{Key}"/>>
    public string CorrelationId => StationName;
}