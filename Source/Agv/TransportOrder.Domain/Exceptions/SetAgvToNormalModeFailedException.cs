namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

/// <summary>
/// Represents the exception raised, when setting an AGV to normal mode failed.
/// </summary>
public class SetAgvToNormalModeFailedException : Exception
{
    /// <inheritdoc cref="SetAgvToNormalModeFailedException"/>>
    public SetAgvToNormalModeFailedException(Guid agvCorrelationId) : base($"Could not set AGV {agvCorrelationId} with paused action to normal mode. At least one station of the order is locked")
    {
    }
}