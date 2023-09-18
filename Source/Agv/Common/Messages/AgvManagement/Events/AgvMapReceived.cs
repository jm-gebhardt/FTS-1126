using Gebhardt.StoreWare.Agv.Common.Contracts;
using JetBrains.Annotations;
using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
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
    public AgvMap AgvMap { get; }

    /// <summary>
    /// Build message
    /// </summary>
    /// <param name="correlationId">The correlation ID of the AGV</param>
    /// <param name="agvId">Identifier of the AGV.</param>
    /// <param name="agvMap">Map of the AGV</param>
    public AgvMapReceived(Guid correlationId, string agvId, AgvMap agvMap)
    {
        CorrelationId = correlationId;
        AgvId = agvId;
        AgvMap = agvMap;
    }
}