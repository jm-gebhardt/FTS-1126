using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.Common.Exceptions;

/// <summary>
/// Agv data was not found
/// </summary>
public class AgvDataNotFoundException : NotFoundException
{
    /// <summary>
    /// Agv data was not found for Agv with Agv correlation Id
    /// </summary>
    public AgvDataNotFoundException(IAgv agv) 
        : base(nameof(AgvData), nameof(agv.CorrelationId), agv.CorrelationId)
    {
    }
}