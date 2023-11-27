namespace Gebhardt.StoreWare.Agv.Common.Exceptions;

/// <summary>
/// Represents errors that occur when the request to an e.g. request handler was not valid
/// </summary>
public class BadRequestException : Exception
{
    /// <summary>
    /// <inheritdoc cref="BadRequestException"/>
    /// </summary>
    /// <param name="reason"></param>
    public BadRequestException(string reason) : base(reason)
    {
    }
}