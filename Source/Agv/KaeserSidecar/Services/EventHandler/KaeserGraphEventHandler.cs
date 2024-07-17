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
    IConsumer<TriggerAssignedToNode>
{
    /// <summary>
    /// Handles the <see cref="TriggerAssignedToNode"/> message.
    /// </summary>
    public Task Consume(ConsumeContext<TriggerAssignedToNode> context)
    {
        configuration.UpdateTriggerPointNodeId(context.Message.TriggerName, context.Message.NodeId);
        return Task.CompletedTask;
    }
}