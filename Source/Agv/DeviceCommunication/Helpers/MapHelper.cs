using System.Globalization;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Exceptions;
using SkiaSharp;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Helpers;

/// <summary>
/// Helper class for map related stuff.
/// </summary>
public static class MapHelper
{
    /// <summary>
    /// Gets the map size from the given map value.
    /// </summary>
    /// <param name="mapValue"></param>
    /// <returns></returns>
    public static (int Width, int Height) GetMapDimensions(byte[] mapValue)
    {
        SKImage image;
        if (mapValue is { Length: >= 1 } && (image = SKImage.FromEncodedData(mapValue)) != null)
        {
            return (image.Width, image.Height);
        }

        throw new MapCreationException(mapValue);
    }

    /// <summary>
    /// Extract the scale up factor, x offset and y offset from the given filename.
    /// Structure: "mapName_scaleUp_xOffset_yOffset.png"
    /// </summary>
    /// <param name="filename"></param>
    /// <param name="throwException"></param>
    /// <returns></returns>
    public static (float scaleUpFactor, float xOffset, float yOffset) ParseFilename(string filename, bool throwException = false)
    {
        float scaleUpFactor = 1;
        float xOffset = 0;
        float yOffset = 0;
        string[] parts = Path.GetFileNameWithoutExtension(filename).Split('_');

        var invariantCulture = CultureInfo.InvariantCulture;

        if (parts.Length != 4 ||
            !float.TryParse(parts[1], invariantCulture, out scaleUpFactor) ||
            !float.TryParse(parts[2], invariantCulture, out xOffset) ||
            !float.TryParse(parts[3], invariantCulture, out yOffset))
        {
            if (throwException)
            {
                throw new FormatException("Filename parameters are not valid numbers.");
            }
        }

        return (scaleUpFactor, xOffset, yOffset);
    }
}