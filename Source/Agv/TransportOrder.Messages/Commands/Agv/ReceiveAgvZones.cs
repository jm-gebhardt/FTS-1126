using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.Contracts;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to receive agv zones.
/// </summary>
public record ReceiveAgvZones : CorrelatedBy<Guid>, IAgv
{
    /// <inheritdoc cref="IAgv.CorrelationId" />
    public Guid CorrelationId { get; }

    /// <inheritdoc />
    public string SerialNumber { get; }

    /// <summary>
    /// The agv zones.
    /// </summary>
    public ICollection<AgvZone> AgvZones { get; }

    /// <inheritdoc cref="ReceiveAgvZones"/>>
    public ReceiveAgvZones(Guid correlationId, string serialNumber, ICollection<AgvZone> agvZones)
    {
        CorrelationId = correlationId;
        SerialNumber = serialNumber;
        AgvZones = agvZones;
    }
}