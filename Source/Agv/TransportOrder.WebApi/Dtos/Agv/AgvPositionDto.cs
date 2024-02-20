using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;

/// <summary>
/// Represents the position of an AGV.
/// </summary>
public record AgvPositionDto
{
    /// <summary>Returns true, when the position is known and valid.</summary>
    public bool PositionInitialized { get; init; }

    /// <summary>Current X-position [m].</summary>
    public float X { get; init; }

    /// <summary>Current Y-position [m].</summary>
    public float Y { get; init; }

    /// <summary>Current Z-position [m].</summary>
    public float Z { get; init; }

    /// <summary>Current angle (rotation) [rad].</summary>
    public float Theta { get; init; }

    /// <summary>Deviation from the responded position [m].</summary>
    public float? DeviationRange { get; init; }

    /// <inheritdoc cref="AgvPositionDto"/>>
    /// <param name="positionInitialized">Returns true, when the position is known and valid.</param>
    /// <param name="x">Current X-position [m].</param>
    /// <param name="y">Current Y-position [m].</param>
    /// <param name="z">Current Z-position [m].</param>
    /// <param name="theta">Current angle (rotation) [rad].</param>
    /// <param name="deviationRange">Deviation from the responded position [m].</param>
    public AgvPositionDto(bool positionInitialized, float x, float y, float z, float theta, float? deviationRange)
    {
        PositionInitialized = positionInitialized;
        X = x;
        Y = y;
        Z = z;
        Theta = theta;
        DeviationRange = deviationRange;
    }

    /// <summary>
    /// ctor called by AutoMapper.
    /// </summary>
    [PublicAPI]
    public AgvPositionDto()
    {
    }
}