using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace TransportOrder.Domain.Exceptions;

/// <summary>
/// Agv data was not found
/// </summary>
public class AgvDataNotFoundException : NotFoundException
{
    /// <summary>
    /// Agv data was not found for Agv with Agv correlation Id
    /// </summary>
    public AgvDataNotFoundException(Guid correlationId)
        : base(nameof(AgvData), nameof(correlationId), correlationId)
    {
    }
}