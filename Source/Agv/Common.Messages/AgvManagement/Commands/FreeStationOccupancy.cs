using MassTransit;

namespace Gebhardt.Acc.Common.Messages.AgvManagement.Commands;

/// <summary>
/// Provides a command to remove the station from the station occupancy.
/// </summary>
/// <param name="NodeId">The node Id of the station.</param>
/// <param name="AgvCorrelationId">The correlation id of the agv.</param>
public record FreeStationOccupancy(Guid NodeId, Guid AgvCorrelationId) : CorrelatedBy<Guid>
{
    /// <inheritdoc cref="CorrelatedBy{Key}"/>>
    public Guid CorrelationId => NodeId;
}