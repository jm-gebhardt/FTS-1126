using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using Gebhardt.Acc.PrismatCommunication.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Domain.Services;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands;
using Gebhardt.Acc.PrismatCommunication.Messages.Events;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.PrismatCommunication.Services.CommandHandler;

/// <summary>
/// Provides handlers for host order related commands.
/// </summary>
[UsedImplicitly]
public class HostOrderCommandHandler(ILogger<HostOrderCommandHandler> logger, IHostOrderRepository hostOrderRepository, IHostCommunicator hostCommunicator, KaeserGraphConfiguration configuration) :
    IConsumer<CreateHostOrder>,
    IConsumer<HandleHostOrderStart>,
    IConsumer<FinishHostOrder>,
    IConsumer<PassDecisionPoint>,
    IConsumer<QueuePickDropActionGroupForAgv>,
    IConsumer<PassLoadTriggerPoint>,
    IConsumer<QueueActionGroupWithUnknownDropTargetForAgv>

{
    /// <summary>
    /// Handles the <see cref="CreateHostOrder"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<CreateHostOrder> context)
    {
        var orderDetails = context.Message.OrderDetails;
        string hu = context.Message.Hu;
       
        var hostOrder = new HostOrder(orderDetails.CorrelationId, orderDetails.OrderId, hu, orderDetails.CreationDate);
        hostOrder.AddAction(orderDetails.Source, ActionType.Pick);
        hostOrder.AddAction(orderDetails.Destination, ActionType.Drop);

        hostOrderRepository.Add(hostOrder);
        await hostOrderRepository.SaveAsync();
        logger.LogInformation("Host order for order {OrderId} created", orderDetails.OrderId);

        // Short circuit
        var actionGroup = hostOrder.TryCreatePickDropActionGroup(configuration, orderDetails);
        if (actionGroup != null)
        {
            await context.Publish(new QueueActionGroup(actionGroup, false));
            return;
        }

        // Long circuit
        if (hostOrder.HasUnknownPickTarget(configuration))
        {
            // Check if there are agvs that are to be selected to handle this order directly after dropping
            if (await hostOrderRepository.HasDropOrdersHeadingTowardsDecisionPoint())
            {
                return;
            }  
            // Call for any idle agv to handle this order
            actionGroup = hostOrder.CreateMovePickDropActionGroup(configuration, orderDetails);
            await context.Publish(new QueueActionGroup(actionGroup, false));
            return;
        }

        if (hostOrder.HasUnknownDropTarget(configuration))
        {
            // Check if there are agvs that are to be selected to handle this order directly after dropping
            if (await hostOrderRepository.HasDropOrdersHeadingTowardsLoadTriggerPoint())
            {
                return;
            }
            // Call for any idle agv to handle this order
            actionGroup = hostOrder.CreatePickMoveDropActionGroup(configuration);
            await context.Publish(new QueueActionGroup(actionGroup, false));
        }
    }

    /// <summary>
    /// Handles the <see cref="HandleHostOrderStart"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<HandleHostOrderStart> context)
    {
        var (correlationId, assignedAgv, timestamp) = context.Message;
        var hostOrder = await hostOrderRepository.GetByIdAsync(correlationId);
        hostOrder.Start(timestamp, assignedAgv);
        await hostOrderRepository.UpdateAsync(hostOrder);
    }

    /// <summary>
    /// Handles the <see cref="FinishHostOrder"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<FinishHostOrder> context)
    {
        var (correlationId, timestamp) = context.Message;
        var hostOrder = await hostOrderRepository.GetByIdAsync(correlationId);
        hostOrder.Finish(timestamp);
        await hostOrderRepository.UpdateAsync(hostOrder);
        await hostCommunicator.ReportOrderCompletion(hostOrder.Hu, hostOrder.OrderId, hostOrder.Actions.Last().Node);
    }

    /// <summary>
    /// Handles the <see cref="PassDecisionPoint"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<PassDecisionPoint> context)
    {
        (var correlationId, Guid? assignedAgv, string targetNodeId) = context.Message;
        var hostOrder = await hostOrderRepository.GetByIdAsync(correlationId);
        hostOrder.PassTriggerPoint();
        await hostOrderRepository.UpdateAsync(hostOrder);
        await context.Publish(new DecisionPointPassed(assignedAgv!.Value, targetNodeId));
    }

    /// <summary>
    /// Handles the <see cref="QueuePickDropActionGroupForAgv"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<QueuePickDropActionGroupForAgv> context)
    {
        (var correlationId, var assignedAgv, string pickNodeId) = context.Message;
        var hostOrder = await hostOrderRepository.GetByIdAsync(correlationId);
        var actionGroup = hostOrder.CreatePickDropActionGroupForAgv(pickNodeId, assignedAgv, configuration);
        await hostOrderRepository.UpdateAsync(hostOrder);
        await context.Publish(new QueueActionGroup(actionGroup, false));
    }

    /// <summary>
    /// Handles the <see cref="PassLoadTriggerPoint"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<PassLoadTriggerPoint> context)
    {
        var (assignedAgv, correlationId) = context.Message;
        if (correlationId != default)
        {
            var hostOrder = await hostOrderRepository.GetByIdAsync(correlationId);
            hostOrder.PassTriggerPoint();
            await hostOrderRepository.UpdateAsync(hostOrder);
        }
   
        await context.Publish(new LoadTriggerPointPassed(assignedAgv));
    }

    /// <summary>
    /// Handles the <see cref="QueueActionGroupWithUnknownDropTargetForAgv"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<QueueActionGroupWithUnknownDropTargetForAgv> context)
    {
        var (correlationId, assignedAgv) = context.Message;
        var hostOrder = await hostOrderRepository.GetByIdAsync(correlationId);
        var actionGroup = hostOrder.CreatePickMoveDropActionGroup(configuration, assignedAgv);
        await hostOrderRepository.UpdateAsync(hostOrder);
        await context.Publish(new QueueActionGroup(actionGroup, false));
    }
}