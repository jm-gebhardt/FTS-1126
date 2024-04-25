using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

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
    public AgvStatusNotFoundException(Agv agv)
        : base(nameof(AgvStatus), nameof(agv.CorrelationId), agv.CorrelationId)
    {
    }
}