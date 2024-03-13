namespace Gebhardt.StoreWare.Agv.Common.Exceptions;

/// <summary>
/// There are currently no nodes available.
/// </summary>
public class NoNodesAvailableException : Exception
{
    /// <inheritdoc cref="NoNodesAvailableException"/>
    /// <param name="description">What nodes were you looking for?</param>
    public NoNodesAvailableException(string description) : base(description)
    {
    }
}