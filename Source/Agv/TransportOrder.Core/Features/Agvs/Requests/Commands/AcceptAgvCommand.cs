using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Commands
{
    /// <summary>
    /// Represents the AcceptAgv request
    /// </summary>
    public record AcceptAgvCommand(Guid CorrelationId) : CorrelatedBy<Guid>;
}
