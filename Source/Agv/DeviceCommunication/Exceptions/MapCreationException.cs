namespace Gebhardt.Acc.DeviceCommunication.Exceptions;

/// <summary>
/// Exception for map creation
/// </summary>
public class MapCreationException : Exception
{
    /// <summary>
    /// Exception for map creation
    /// </summary>
    /// <param name="mapValue"></param>
    public MapCreationException(byte[] mapValue) : base($"Map creation failed for map with size: {mapValue.Length}")
    {
    }
}