using JetBrains.Annotations;
using SkiaSharp;

namespace TransportOrder.Domain.Models.Map;

/// <summary>
/// class representing the Map table in Database
/// </summary>
public class Map 
{
    /// <summary>
    /// The correlation id o the map.
    /// </summary>
    public Guid CorrelationId { get; set; }

    /// <summary>
    /// MapId
    /// </summary>
    public string MapId { get; set; }


    private byte[]? _mapValue;
    /// <summary>
    /// Map in form bytes
    /// </summary>
    [UsedImplicitly]
    public byte[]? MapValue { 
        get => _mapValue; 
        set
        { 
            _mapValue = value;
            if (_mapValue is { Length: >= 1 })
            {
                SetDimensions(_mapValue);
                return;
            }
            Width = 0;
            Height = 0;
        }
    }

    /// <summary>
    /// Timestamp of last modification
    /// </summary>
    [UsedImplicitly]
    public DateTime ModificationDate { get; set; }

    /// <summary>
    /// The resolution of the maps image. Expressed as how many [m] per pixel are represented
    /// </summary>
    public double Resolution { get; set; }

    /// <summary>
    /// The x offset of the map in [m] where the mapped graph coordinate system starts. Origin is the left upper corner.
    /// </summary>
    public double XOffset { get; set; }

    /// <summary>
    /// The y offset of the map in [m] where the mapped graph coordinate system starts. Origin is the left upper corner.
    /// </summary>
    public double YOffset { get; set; }

    /// <summary>
    /// The width of the map image [pixel].
    /// </summary>
    public int Width { get; private set; }

    /// <summary>
    /// The height of the map image [pixel].
    /// </summary>
    public int Height { get; private set; }

    /// <summary>
    /// Initializes a new <see cref="Map"/> instance.
    /// </summary>
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

    private void SetDimensions(byte[] mapValue)
    {
        var image = SKImage.FromEncodedData(mapValue);
        Width = image.Width;
        Height = image.Height;
    }
}