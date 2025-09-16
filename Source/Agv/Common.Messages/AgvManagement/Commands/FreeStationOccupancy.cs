using MassTransit;

namespace Gebhardt.Acc.Common.Messages.AgvManagement.Commands;

/// <summary>
/// Provides a command to remove the station from the station occupancy.
/// </summary>
/// <param name="StationId">The station Id.</param>
/// <param name="AgvCorrelationId">The correlation id of the agv.</param>
public record FreeStationOccupancy(Guid StationId, Guid AgvCorrelationId) : CorrelatedBy<Guid>
{
    /// <inheritdoc cref="CorrelatedBy{Key}"/>>
    public Guid CorrelationId => StationId;
}