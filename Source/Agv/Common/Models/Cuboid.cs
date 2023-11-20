namespace Gebhardt.StoreWare.Agv.Common.Models;

/// <summary>
/// 3D dimension of a cuboid
/// </summary>
public record Cuboid
{
    /// <summary>
    /// Width [m]
    /// </summary>
    public float Width { get; init; }

    /// <summary>
    /// Length [m]
    /// </summary>
    public float Length { get; init; }

    /// <summary>
    /// Height [m]
    /// (Optional)
    /// </summary>
    public float? Height { get; init; }

    /// <inheritdoc cref="Cuboid"/>
    /// <param name="width">width [m]</param>
    /// <param name="length">length [m]</param>
    /// <param name="height">height [m] (Optional)</param>
    public Cuboid(float width, float length, float? height)
    {
        Height = height;
        Length = length;
        Width = width;
    }
}