using MassTransit;

namespace Gebhardt.Acc.Common.Messages.ActionGroups.Commands;

/// <summary>
/// Provides a command to create an action group.
/// </summary>
/// <param name="ActionGroup">The action group to add to the system</param>
public record CreateActionGroup(ActionGroup ActionGroup) : CorrelatedBy<Guid>
{
    /// <inheritdoc />
    public Guid CorrelationId => ActionGroup.CorrelationId;
}