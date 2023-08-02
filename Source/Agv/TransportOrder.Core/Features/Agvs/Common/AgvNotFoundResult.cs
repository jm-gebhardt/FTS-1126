using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Common
{
    /// <summary>
    /// Defines a result for the case that an agv for a request was not found
    /// </summary>
    public record AgvNotFoundResult(Guid CorrelationId) : CorrelatedBy<Guid>;
}
