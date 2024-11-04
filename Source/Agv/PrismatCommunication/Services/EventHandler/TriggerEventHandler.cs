using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.PrismatCommunication.Services.EventHandler;

/// <summary>
/// Handles trigger related events
/// </summary>
[UsedImplicitly]
public class TriggerEventHandler(KaeserGraphConfiguration configuration): 
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
        // Simulation for prismat communication needs in-memory configuration
        // with all triggers "assigned" to a node so order creation is started. So dummy node name is set.
        configuration.UpdateTriggerPointNodeId(context.Message.TriggerName, string.Join("Area-", context.Message.AreaId));
        return Task.CompletedTask;
    }
}