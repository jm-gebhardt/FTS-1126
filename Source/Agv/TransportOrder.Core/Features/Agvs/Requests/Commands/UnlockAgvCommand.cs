using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Commands
{
    /// <summary>
    /// Represents the UnlockAgv request
    /// </summary>
    public record UnlockAgvCommand(Guid CorrelationId) : CorrelatedBy<Guid>;
}
