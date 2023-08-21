namespace Gebhardt.StoreWare.Agv.Common.Exceptions;

/// <summary>
/// Represents errors that occur when the object is not in correct state
/// </summary>
public class NotInCorrectStateException : Exception
{
    /// <inheritdoc cref="NotInCorrectStateException"/>
    /// <param name="message">Exception message</param>
    public NotInCorrectStateException(string message) : base(message)
    {

    }
}