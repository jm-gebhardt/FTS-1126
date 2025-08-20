using MassTransit;

namespace Gebhardt.Acc.Common.Messages.AgvManagement.Commands;

/// <summary>
/// Provides a command to remove the station from the station occupancy.
/// </summary>
/// <param name="Station">The station name.</param>
/// <param name="AgvCorrelationId">The correlation id of the agv.</param>
public record FreeStationOccupancy(string Station, Guid AgvCorrelationId) : CorrelatedBy<string>
{
    /// <inheritdoc cref="CorrelatedBy{Key}"/>>
    public string CorrelationId => Station;
}