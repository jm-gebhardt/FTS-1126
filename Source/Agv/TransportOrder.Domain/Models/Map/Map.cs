namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Map;

/// <summary>
/// class representing the Map table in Database
/// </summary>
public record Map
{
    /// <summary>
    /// The correlation id o the map.
    /// </summary>
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// MapId
    /// </summary>
    public string MapId { get; init; }

    /// <summary>
    /// Map in form bytes
    /// </summary>
    public byte[]? MapValue { get; init; }

    /// <summary>
    /// Timestamp of last modification
    /// </summary>
    public DateTime ModificationDate { get; init; }

    /// <summary>
    /// The resolution of the maps image. Expressed as how many [m] per pixel are represented
    /// </summary>
    public double Resolution { get; init; }

    /// <summary>
    /// The x offset of the map in [m] where the mapped graph coordinate system starts. Origin is the left upper corner.
    /// </summary>
    public double XOffset { get; init; }

    /// <summary>
    /// The y offset of the map in [m] where the mapped graph coordinate system starts. Origin is the left upper corner.
    /// </summary>
    public double YOffset { get; init; }

    /// <summary>
    /// The width of the map [px]
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    /// The height of the map [px]
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    /// Initializes a new <see cref="Map"/> instance.
    /// </summary>
    public Map(Guid correlationId, string mapId, byte[]? mapValue, int width, int height, double resolution, double xOffset, double yOffset, DateTime modificationDate)
    {
        CorrelationId = correlationId;
        MapId = mapId;
        MapValue = mapValue;
        Width = width;
        Height = height;
        Resolution = resolution;
        XOffset = xOffset;
        YOffset = yOffset;
        ModificationDate = modificationDate;
    }
}