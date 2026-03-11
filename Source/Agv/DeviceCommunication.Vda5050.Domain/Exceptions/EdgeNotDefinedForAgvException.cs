namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;

/// <summary>
/// Provides an exception raise when an edge has not been defined in the agv graph.
/// </summary>
public class EdgeNotDefinedForAgvException: Exception
{
    /// <inheritdoc/>
    public EdgeNotDefinedForAgvException(Guid edgeId, Guid agvCorrelationId) : base($"Action edge {edgeId} not defined in graph of agv {agvCorrelationId}") { }
}