using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.Common.Exceptions;

/// <summary>
/// LastNodeId of Agv is null
/// </summary>
public class AgvLocationUnknownException : Exception
{
    /// <see cref="AgvLocationUnknownException"/>>
    /// <param name="agv">The Agv where the LastNodeId is null</param>
    [UsedImplicitly]
    public AgvLocationUnknownException(IAgv agv) : base($"Last node of Agv: {agv} is unknown")
    {
        
    }
    
    /// <see cref="AgvLocationUnknownException"/>>
    /// <param name="agvCorrelationId">The CorrelationId of the Agv</param>
    public AgvLocationUnknownException(Guid agvCorrelationId) : base($"Last node of Agv with CorrelationId: {agvCorrelationId} is unknown")
    {
        
    }   
}