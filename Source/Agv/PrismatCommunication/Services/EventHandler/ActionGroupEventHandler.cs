using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.PrismatCommunication.Services.EventHandler;

/// <summary>
/// Provides handlers for action group related events.
/// </summary>
[UsedImplicitly]
public class ActionGroupEventHandler:
    IConsumer<ActionGroupCompleted>
{
    /// <summary>
    /// Handles the <see cref="ActionGroupCompleted"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<ActionGroupCompleted> context)
    {
        var (actionGroup, correlationId, timestamp) = context.Message;
        if (actionGroup.Creator is ActionGroupCreator.Wms or ActionGroupCreator.Simulation)
        {
            await context.Publish(new FinishHostOrder(correlationId, timestamp));
        }
    }
}