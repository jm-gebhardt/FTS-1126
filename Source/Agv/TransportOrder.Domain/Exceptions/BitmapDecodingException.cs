namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

/// <summary>
/// Represents errors that occur during bitmap decoding.
/// </summary>
public class BitmapDecodingException : Exception
{
    /// <inheritdoc cref="MapNotFoundException"/>
    public BitmapDecodingException(string message) : base(message)
    {
    }
}