using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

/// <summary>
/// Represents the exception thrown when a node was not of expected type.
/// </summary>
[UsedImplicitly]
public class InvalidNodeTypeException : Exception
{
    /// <summary>
    /// Initializes a new <see cref="InvalidNodeTypeException"/> instance.
    /// </summary>
    public InvalidNodeTypeException(string nodeId, string? type, string expectedType)
        : base($"Node {nodeId} has invalid type{(type != null ? $" {type}" : "")}, expected {expectedType
        }")
    {
    }
}