using Gebhardt.StoreWare.Agv.Common.Contracts;
using JetBrains.Annotations;
using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;

/// <summary>
/// Command to request the map of an AGV.
/// </summary>
[PublicAPI]
public record RequestAgvMap : CorrelatedBy<Guid>, IAgv
{
    /// <inheritdoc cref="IAgv.CorrelationId" />
    public Guid CorrelationId { get; }

    /// <inheritdoc />
    public string SerialNumber { get; }

    /// <summary>
    /// The correlation ID of the requested map
    /// </summary>
    public Guid MapCorrelationId { get; }

    /// <summary>
    /// The human readable unique ID if the requested map
    /// </summary>
    public string MapId { get; }

    /// <summary>
    /// Build message
    /// </summary>
    /// <param name="correlationId">The correlation Id of the Agv.</param>
    /// <param name="serialNumber">The serial number of the agv.</param>
    /// <param name="mapCorrelationId"></param>
    /// <param name="mapId"></param>
    public RequestAgvMap(Guid correlationId, string serialNumber, Guid mapCorrelationId, string mapId)
    {
        CorrelationId = correlationId;
        SerialNumber = serialNumber;
        MapCorrelationId = mapCorrelationId;
        MapId = mapId;
    }
}