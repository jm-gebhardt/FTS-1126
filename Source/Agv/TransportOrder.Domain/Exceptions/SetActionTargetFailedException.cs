namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

/// <summary>
/// Represents the exception raised, when setting an action target failed.
/// </summary>
public class SetActionTargetFailedException: Exception
{
    /// <inheritdoc cref="SetActionTargetFailedException"/>>
    public SetActionTargetFailedException(string targetNode): base($"Could not set target node {targetNode}, target was already configured"){}
}