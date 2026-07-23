namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;

/// <summary>
/// Provides an exception raised when a route step names an action type that the AGV's fact sheet does not
/// declare support for.
/// </summary>
public class RouteActionNotSupportedByAgvException : Exception
{
    /// <inheritdoc/>
    public RouteActionNotSupportedByAgvException(string actionType, Guid agvCorrelationId)
        : base($"Action type '{actionType}' is not supported by the fact sheet of agv {agvCorrelationId}")
    {
    }
}
