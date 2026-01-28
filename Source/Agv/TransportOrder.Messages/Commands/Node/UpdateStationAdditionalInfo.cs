using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Node;

/// <summary>
/// Provides a command to update the additional information of a station.
/// </summary>
/// <param name="Id">The station id.</param>
/// <param name="StationProperties">The new additional information.</param>
public record UpdateStationAdditionalInfo(Guid Id, Dictionary<string, object?> StationProperties) : CorrelatedBy<Guid>
{
    /// <summary>
    /// The correlation id of the message (station id).
    /// </summary>
    public Guid CorrelationId => Id;
}