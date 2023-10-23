namespace Gebhardt.StoreWare.Agv.Common.Exceptions;

/// <summary>
/// LastNodeId of Agv is null
/// </summary>
public class AgvLocationUnknownException : Exception
{
    /// <see cref="AgvLocationUnknownException"/>>
    /// <param name="agvCorrelationId">The CorrelationId of the Agv</param>
    public AgvLocationUnknownException(Guid agvCorrelationId) : base($"Last node of Agv with CorrelationId: {agvCorrelationId} is unknown")
    {
    }
}