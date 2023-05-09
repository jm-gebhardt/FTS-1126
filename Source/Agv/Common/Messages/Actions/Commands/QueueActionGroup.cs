using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.Actions.Commands
{
    /// <summary>
    /// Command to queue an action group
    /// </summary>
    /// <param name="ActionGroup"></param>
    public record QueueActionGroup(ActionGroup ActionGroup) : CorrelatedBy<Guid>
    {
        /// <inheritdoc />
        public Guid CorrelationId => ActionGroup.CorrelationId;
    }
}