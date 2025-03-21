using Gebhardt.Acc.Common.Messages.Contracts;
using MassTransit;

namespace Gebhardt.Acc.Common.Messages.AgvManagement.Commands;

/// <summary>
/// Provides a command to let an agv remove another agv from a zone.
/// </summary>
public record RemoveAgvFromZone : IAgv, CorrelatedBy<Guid>
{
    /// <inheritdoc cref="IAgv.CorrelationId" />
    public Guid CorrelationId { get; }

    /// <inheritdoc />
    public string SerialNumber { get; }

    /// <summary>
    /// The serial number of the agv that is to be removed.
    /// </summary>
    public string AgvToRemove { get; }

    /// <summary>
    /// The id of the zone the agv is to be removed from.
    /// </summary>
    public string ZoneId { get; }

    /// <inheritdoc cref="RemoveAgvFromZone"/>>
    public RemoveAgvFromZone(Guid correlationId, string serialNumber, string agvToRemove, string zoneId)
    {
        CorrelationId = correlationId;
        SerialNumber = serialNumber;
        AgvToRemove = agvToRemove;
        ZoneId = zoneId;
    }
}