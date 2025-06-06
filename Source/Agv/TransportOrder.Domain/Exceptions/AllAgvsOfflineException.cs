namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

/// <summary>
/// Represents an exception thrown when all agvs are offline even though there were some expected to be online.
/// </summary>
public class AllAgvsOfflineException : Exception
{
    /// <inheritdoc cref="AllAgvsOfflineException"/>
    public AllAgvsOfflineException() : base("All agvs are offline")
    {
    }
}