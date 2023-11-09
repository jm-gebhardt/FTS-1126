using JetBrains.Annotations;
using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

/// <summary>
/// AGV Map
/// </summary>
[PublicAPI]
public record AgvMap : CorrelatedBy<Guid>
{
    /// <inheritdoc/>
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// The human readable unique ID if the requested map
    /// </summary>
    public string MapId { get; init; }

    /// <summary>
    /// Timestamp of last modification
    /// </summary>
    public DateTime ModificationDate { get; init; }

    /// <summary>
    /// The Base64 encoded image data as string
    /// </summary>
    public string ImageData { get; init; }

    /// <summary>
    /// The Width of the map [px]
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    /// The Height of the map [px]
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    /// Resolution of the Image in [m/px].
    /// </summary>
    public float Resolution { get; init; }

    /// <summary>
    /// Distance from the left bound to the origin of the graph coordinates.
    /// When the origin is left from the left bound this value is negative.
    /// </summary>
    public float XOffset { get; init; }

    /// <summary>
    /// Distance from the top bound to the origin of the graph coordinates.
    /// When the origin is upper to the top bound this value is negative.
    /// </summary>
    public float YOffset { get; init; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="correlationId">Map correlation ID</param>
    /// <param name="mapId">The human readable unique ID if the requested map</param>
    /// <param name="modificationDate">Timestamp of last modification.</param>
    /// <param name="imageData">The base64 encoded image data as string</param>
    /// <param name="width">The width of the map [px]</param>
    /// <param name="height">The height of the map [px]</param>
    /// <param name="resolution">Resolution of the Image in [m/px].</param>
    /// <param name="xOffset">Distance from the left bound to the origin of the graph coordinates.</param>
    /// <param name="yOffset">Distance from the top bound to the origin of the graph coordinates.</param>
    public AgvMap(Guid correlationId,
        string mapId,
        DateTime modificationDate,
        string imageData,
        int width,
        int height,
        float resolution,
        float xOffset,
        float yOffset)
    {
        CorrelationId = correlationId;
        MapId = mapId;
        ModificationDate = modificationDate;
        ImageData = imageData;
        Width = width;
        Height = height;
        Resolution = resolution;
        XOffset = xOffset;
        YOffset = yOffset;
    }
}
