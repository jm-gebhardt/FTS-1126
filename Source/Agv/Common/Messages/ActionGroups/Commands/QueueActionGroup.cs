using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Commands;

/// <summary>
/// Command to queue an ActionGroup
/// </summary>
/// <param name="ActionGroup">The ActionGroup to add to the system</param>
/// <param name="ThrowOnInvalid">If true the ActionGroup won't get added to the system and an exception will be thrown</param>
public record QueueActionGroup(ActionGroup ActionGroup, bool ThrowOnInvalid) : CorrelatedBy<Guid>
{
    /// <inheritdoc />
    public Guid CorrelationId => ActionGroup.CorrelationId;
}