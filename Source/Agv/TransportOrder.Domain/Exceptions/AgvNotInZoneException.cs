namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

/// <summary>
/// Represents an exception thrown when an agv is not in a zone it was expected to be in.
/// </summary>
public class AgvNotInZoneException : Exception
{
    /// <summary>
    /// Represents an exception thrown when an agv is not in a zone it was expected to be in.
    /// </summary>
    /// <param name="zoneId">The zone that the agv was expected to be in.</param>
    /// <param name="agvId">The id of the agv.</param>
    public AgvNotInZoneException(string zoneId, Guid agvId) : base($"Agv {agvId} not in zone {zoneId}")
    {
    }
}