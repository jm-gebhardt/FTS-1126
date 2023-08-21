namespace Gebhardt.StoreWare.Agv.Common.Utilities;

/// <summary>
/// A collection of useful mathematical functions
/// </summary>
public static class Mathematics
{
    /// <summary>
    /// Converts an angle in degrees to radians
    /// </summary>
    /// <param name="angle">The angle in degrees</param>
    /// <returns></returns>
    public static float ConvertToRadians(float angle)
    {
        return (float)(Math.PI / 180) * angle;
    }
}