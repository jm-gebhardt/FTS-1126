namespace Gebhardt.Acc.Common.Exceptions;

/// <summary>
/// Request failed
/// </summary>
public class RequestFailedException : Exception
{
    /// <param name="description"></param>
    /// <inheritdoc cref="RequestFailedException"/>
    public RequestFailedException(string description) : base(description)
    {
    }
}