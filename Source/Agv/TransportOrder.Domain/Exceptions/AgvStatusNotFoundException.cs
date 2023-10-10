using Gebhardt.StoreWare.Agv.Common.Exceptions;
using JetBrains.Annotations;
using AgvStatus = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv.AgvStatus;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;

/// <summary>
/// Agv status was not found
/// </summary>
[UsedImplicitly]
public class AgvStatusNotFoundException : NotFoundException
{
    /// <summary>
    /// Agv Status not found for Agv
    /// </summary>
    /// <param name="agv"></param>
    public AgvStatusNotFoundException(Models.Agv.Agv agv) 
        : base(nameof(AgvStatus), nameof(agv.CorrelationId), agv.CorrelationId)
    {
    }
}