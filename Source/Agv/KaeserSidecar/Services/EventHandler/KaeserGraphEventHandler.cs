using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.KaeserSidecar.Services.EventHandler;

/// <summary>
/// Handles kaeser graph related events
/// </summary>
[UsedImplicitly]
public class KaeserGraphEventHandler(KaeserGraphConfiguration configuration) :
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
        string nodeId = "AREA-" + context.Message.AreaId;
        configuration.UpdateTriggerPointNodeId(context.Message.TriggerName, nodeId);
        return Task.CompletedTask;
    }
}