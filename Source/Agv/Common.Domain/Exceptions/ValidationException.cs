namespace Gebhardt.Acc.Common.Domain.Exceptions;

/// <summary>
/// Represents errors that occur when the request to an e.g. request handler was not valid
/// </summary>
public class ValidationException : Exception
{
    /// <summary>
    /// <inheritdoc cref="ValidationException"/>
    /// </summary>
    /// <param name="reason"></param>
    public ValidationException(string reason) : base(reason)
    {
    }
}