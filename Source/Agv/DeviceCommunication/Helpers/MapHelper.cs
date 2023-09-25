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
        if (mapValue is {Length: >= 1} && (image = SKImage.FromEncodedData(mapValue)) != null)
        {
            return (image.Width, image.Height);
        }

        throw new MapCreationException(mapValue);
    }
}