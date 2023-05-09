using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.Common.Exceptions;

/// <summary>
/// The Graph of the Agv is null
/// </summary>
public class AgvGraphNotDefinedException : NotFoundException
{
    /// <see cref="AgvGraphNotDefinedException"/>>
    public AgvGraphNotDefinedException(IAgv agv)
        : base(nameof(AgvGraph), nameof(agv.CorrelationId), agv.CorrelationId)
    {
    }
    
    /// <summary>
    /// Agv Graph with Agv correlationId was not found
    /// </summary>
    /// <param name="correlationId">The correlation Id of the Agv</param>
    [JetBrains.Annotations.UsedImplicitly]
    public AgvGraphNotDefinedException(Guid correlationId) 
        : base(nameof(AgvGraph), nameof(correlationId), correlationId)
    {
    }
}