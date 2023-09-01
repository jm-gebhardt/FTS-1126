
namespace TransportOrder.Domain.Exceptions;

/// <summary>
/// There are no nodes available
/// </summary>
public class NoNodesAvailableException : Exception
{
    /// <summary>
    /// Ctor
    /// </summary>
    /// <param name="description">What nodes were you looking for?</param>
    public NoNodesAvailableException(string description) : base(description)
    {
    }
}