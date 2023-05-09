using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events
{
    /// <summary>
    /// Event sent when an AGV needs to be moved to a charging station, because it's Soc is decreased (below low)
    /// <param name="CorrelationId">AGV CorrelationId</param>
    /// </summary>
    public record AgvNeedsCharging(Guid CorrelationId) : CorrelatedBy<Guid>;
}