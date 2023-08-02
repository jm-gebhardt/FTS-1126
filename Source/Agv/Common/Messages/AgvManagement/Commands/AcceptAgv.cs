using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands
{
    /// <summary>
    /// Request an AGV to transition from Discoverd to to Idle state
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record AcceptAgv(Guid CorrelationId) : CorrelatedBy<Guid>;
}
