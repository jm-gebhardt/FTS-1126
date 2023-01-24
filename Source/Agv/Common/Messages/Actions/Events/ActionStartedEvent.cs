using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.Actions.Events
{
    /// <summary>
    /// An event that reports the successful start of an action
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record ActionStartedEvent(Guid CorrelationId) : CorrelatedBy<Guid>;

}
