using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.Contracts;
using MassTransit;

namespace Gebhardt.Acc.Common.Messages.AgvManagement.Events;

/// <summary>
/// Represents the event raised when an agv sent its zones.
/// </summary>
public record AgvZonesReceived : CorrelatedBy<Guid>, IAgv
{
    /// <inheritdoc cref="IAgv.CorrelationId" />
    public Guid CorrelationId { get; }

    /// <inheritdoc />
    public string SerialNumber { get; }

    /// <summary>
    /// The agv zones.
    /// </summary>
    public ICollection<AgvZone> AgvZones { get;  }

    /// <inheritdoc cref="AgvZonesReceived"/>>
    public AgvZonesReceived(Guid correlationId, string serialNumber, ICollection<AgvZone> agvZones)
    {
        CorrelationId = correlationId;
        SerialNumber = serialNumber;
        AgvZones = agvZones;
    }
}