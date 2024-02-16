namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;

/// <summary>
/// Represents the exception thrown when an action group is invalid.
/// </summary>
public class ActionGroupInvalidException : Exception
{
    /// <summary>
    /// Initializes a new <see cref="ActionGroupInvalidException"/> instance.
    /// </summary>
    public ActionGroupInvalidException(string[] reasons) : base($"Action group invalid because: {string.Join(",", reasons)}")
    {
    }
}