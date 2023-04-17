using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.Common.Exceptions;

/// <summary>
/// Agv status was not found
/// </summary>
public class AgvStatusNotFoundException : NotFoundException
{
    /// <summary>
    /// Agv Status not found for Agv
    /// </summary>
    /// <param name="agv"></param>
    public AgvStatusNotFoundException(IAgv agv) 
        : base(nameof(AgvStatus), nameof(agv.CorrelationId), agv.CorrelationId)
    {
    }
}