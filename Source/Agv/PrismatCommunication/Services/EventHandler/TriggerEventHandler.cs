using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.PrismatCommunication.Services.EventHandler;

/// <summary>
/// Handles trigger related events
/// </summary>
[UsedImplicitly]
public class TriggerEventHandler(KaeserGraphConfiguration configuration) :
    IConsumer<TriggerAssignedToNode>,
    IConsumer<TriggerAssignedToArea>
{
    /// <summary>
    /// Handles the <see cref="TriggerAssignedToNode"/> message.
    /// </summary>
    public Task Consume(ConsumeContext<TriggerAssignedToNode> context)
    {
        configuration.UpdateTriggerPointNodeId(context.Message.TriggerName, context.Message.NodeId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the <see cref="TriggerAssignedToArea"/> message.
    /// </summary>
    public Task Consume(ConsumeContext<TriggerAssignedToArea> context)
    {
        // Create a dummy node id from the area id.
        var nodeName = ("AREA-" + context.Message.AreaId).ToGuid();
        configuration.UpdateTriggerPointNodeId(context.Message.TriggerName, nodeName);
        return Task.CompletedTask;
    }
}