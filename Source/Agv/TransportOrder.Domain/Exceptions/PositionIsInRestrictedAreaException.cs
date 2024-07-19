namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

/// <summary>
/// Represents the exception thrown, when a position is in a restricted area.
/// </summary>
public class PositionIsInRestrictedAreaException : Exception
{
    /// <inheritdoc cref="PositionIsInRestrictedAreaException"/>>
    public PositionIsInRestrictedAreaException(float x, float y) : base($"({x},{y}) is located in restricted area")
    {
    }
}