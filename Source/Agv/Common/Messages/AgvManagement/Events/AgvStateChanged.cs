using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events
{
    /// <summary>
    /// An event reporting that the current position of an AGV has changed
    /// </summary>
    /// <param name="CorrelationId">The correlationId of the agv</param>
    /// <param name="Agv">Agv with the new state</param>
    /// <param name="PreviousState">Previous state of the agv</param>
    [JetBrains.Annotations.PublicAPI]
    public record AgvStateChanged(Guid CorrelationId, IAgv Agv, string? PreviousState) : CorrelatedBy<Guid>;
}
