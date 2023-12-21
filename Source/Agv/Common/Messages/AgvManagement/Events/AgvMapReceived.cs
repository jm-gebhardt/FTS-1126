using Gebhardt.StoreWare.Agv.Common.Contracts;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;

/// <summary>
/// An event reporting the retrieval of an AGV map.
/// </summary>
[PublicAPI]
public record AgvMapReceived : CorrelatedBy<Guid>, IAgvId
{
    /// <inheritdoc/>
    public Guid CorrelationId { get; }

    /// <inheritdoc />
    public string AgvId { get; }

    /// <summary>
    /// Map of the AGV
    /// </summary>
    public Map Map { get; }

    /// <summary>
    /// Build message
    /// </summary>
    /// <param name="correlationId">The correlation ID of the AGV</param>
    /// <param name="agvId">Identifier of the AGV.</param>
    /// <param name="map">Map of the AGV</param>
    public AgvMapReceived(Guid correlationId, string agvId, Map map)
    {
        CorrelationId = correlationId;
        AgvId = agvId;
        Map = map;
    }
}