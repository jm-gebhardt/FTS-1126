namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

/// <summary>
/// Represents the exception thrown when a polygon with less than three points is defined.
/// </summary>
public class InvalidPolygonLengthException : Exception
{
    /// <inheritdoc cref="InvalidPolygonLengthException"/>>
    public InvalidPolygonLengthException(): base ("Polygon must have at least three points"){}
}