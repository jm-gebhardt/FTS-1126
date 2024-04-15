using Gebhardt.StoreWare.Agv.Common.Domain.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Messages.Contracts;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;

/// <summary>
/// An event reporting the retrieval of an AGV map.
/// </summary>
[PublicAPI]
public record AgvMapReceived : CorrelatedBy<Guid>, IAgv
{
    /// <inheritdoc cref="IAgv.CorrelationId" />
    public Guid CorrelationId { get; }

    /// <inheritdoc />
    public string SerialNumber { get; }

    /// <summary>
    /// Map of the AGV
    /// </summary>
    public Map Map { get; }

    /// <summary>
    /// Build message
    /// </summary>
    /// <param name="correlationId">The correlation ID of the AGV</param>
    /// <param name="serialNumber">The serial number of the agv.</param>
    /// <param name="map">Map of the AGV</param>
    public AgvMapReceived(Guid correlationId, string serialNumber, Map map)
    {
        CorrelationId = correlationId;
        SerialNumber = serialNumber;
        Map = map;
    }
}