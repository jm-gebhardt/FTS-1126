using Gebhardt.StoreWare.Agv.TransportOrder.Core.Models;
using JetBrains.Annotations;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db.Models;

/// <summary>
/// class representing the Map table in Database
/// </summary>
public record Map : IMap
{
    /// <inheritdoc/>
    public Guid CorrelationId { get; set; }

    /// <inheritdoc/>
    [UsedImplicitly]
    public string MapId { get; set; }

    /// <inheritdoc/>
    [UsedImplicitly]
    public byte[]? MapValue { get; set; }

    /// <inheritdoc/>
    [UsedImplicitly]
    public DateTime ModificationDate { get; set; }

    /// <inheritdoc/>
    public double Resolution { get; set; }

    /// <inheritdoc/>
    public double XOffset { get; set; }

    /// <inheritdoc/>
    public double YOffset { get; set; }

    /// <summary>
    /// Full constructor
    /// </summary>
    /// <param name="correlationId"></param>
    /// <param name="mapId"></param>
    /// <param name="mapValue"></param>
    /// <param name="resolution"></param>
    /// <param name="xOffset"></param>
    /// <param name="yOffset"></param>
    /// <param name="modificationDate"></param>
    public Map(Guid correlationId, string mapId, byte[]? mapValue, double resolution, double xOffset, double yOffset, DateTime modificationDate)
    {
        CorrelationId = correlationId;
        MapId = mapId;
        MapValue = mapValue;
        Resolution = resolution;
        XOffset = xOffset;
        YOffset = yOffset;
        ModificationDate = modificationDate;
    }
}