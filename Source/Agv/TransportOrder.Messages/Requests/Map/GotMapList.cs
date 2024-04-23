using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Map;

/// <summary>
/// Defines a result for the GetMapList request
/// </summary>
public record GotMapList
{
    /// <summary>
    /// <inheritdoc cref="GotMapImage"/>
    /// </summary>
    public List<Map> Maps { get; init; } = null!;
}

/// <summary>
/// Represents the map message content.
/// </summary>
[UsedImplicitly]
public record Map
{
    /// <summary>
    /// Gets the Correlation Id of the map.
    /// </summary>
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// Gets the Map Id.
    /// </summary>
    public string MapId { get; init; }

    /// <summary>
    /// Gets the resolution of the map's image, expressed as how many meters per pixel are represented.
    /// </summary>
    public double Resolution { get; init; }

    /// <summary>
    /// Gets the x offset of the map in meters where the mapped graph coordinate system starts. Origin is the left upper corner.
    /// </summary>
    public double XOffset { get; init; }

    /// <summary>
    /// Gets the y offset of the map in meters where the mapped graph coordinate system starts. Origin is the left upper corner.
    /// </summary>
    public double YOffset { get; init; }

    /// <summary>
    /// Gets the list of AGVs (Automated Guided Vehicles) using this map.
    /// </summary>
    public Guid[] RelatedAgvs { get; init; }

    /// <summary>
    /// Gets the width of the map.
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    /// Gets the height of the map.
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    /// AutoMapper
    /// </summary>
    private Map()
    {
        MapId = null!;
        RelatedAgvs = null!;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Map"/> record with specified properties.
    /// </summary>
    /// <param name="correlationId">The Correlation Id of the map.</param>
    /// <param name="mapId">The Map Id.</param>
    /// <param name="resolution">The resolution of the map's image, in meters per pixel.</param>
    /// <param name="xOffset">The x offset of the map in meters.</param>
    /// <param name="yOffset">The y offset of the map in meters.</param>
    /// <param name="relatedAgvs">The list of AGVs using this map.</param>
    /// <param name="width">The width of the map.</param>
    /// <param name="height">The height of the map.</param>
    public Map(Guid correlationId, string mapId, double resolution, double xOffset, double yOffset, Guid[] relatedAgvs, int width, int height)
    {
        CorrelationId = correlationId;
        MapId = mapId;
        Resolution = resolution;
        XOffset = xOffset;
        YOffset = yOffset;
        RelatedAgvs = relatedAgvs;
        Width = width;
        Height = height;
    }
}