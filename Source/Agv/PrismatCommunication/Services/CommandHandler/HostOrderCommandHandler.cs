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
    IConsumer<CancelHostOrder>,
    IConsumer<HandleHostOrderStart>,
    IConsumer<FinishHostOrder>,
    IConsumer<PassDecisionPoint>,
    IConsumer<QueuePickDropActionGroupForAgv>,
    IConsumer<PassCargoTriggerPoint>,
    IConsumer<PassShortLoopTriggerPoint>,
    IConsumer<QueueActionGroupWithUnknownDropTargetForAgv>,
    IConsumer<FinishPick>

{
    /// <summary>
    /// Handles the <see cref="CreateHostOrder"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<CreateHostOrder> context)
    {
        var orderDetails = context.Message.OrderDetails;
        string hu = context.Message.Hu;
       
        var hostOrder = new HostOrder(orderDetails.CorrelationId, orderDetails.OrderId, hu, orderDetails.CreationDate);

        (Dictionary<InfoToAgvKey, string>? pickInfoToAgv, Dictionary<InfoToAgvKey, string>? dropInfoToAgv) = GeneratePickAndDropInformation(context.Message, configuration);
        logger.LogDebug("pickInfoToAgv: {PickInfoToAgv}", pickInfoToAgv == null ? "empty" : string.Join(", ", pickInfoToAgv.Select(kvp => $"{kvp.Key}: {kvp.Value}")));
        logger.LogDebug("dropInfoToAgv: {DropInfoToAgv}", dropInfoToAgv == null ? "empty" : string.Join(", ", dropInfoToAgv.Select(kvp => $"{kvp.Key}: {kvp.Value}")));

        hostOrder.AddAction(orderDetails.Source, ActionType.Pick, pickInfoToAgv);
        hostOrder.AddAction(orderDetails.Destination, ActionType.Drop, dropInfoToAgv);

        hostOrderRepository.Add(hostOrder);
        await hostOrderRepository.SaveAsync();
        logger.LogInformation("Host order for order {OrderId} created", orderDetails.OrderId);

        await QueueHostOrder(context, hostOrder, orderDetails.CreationDate);
    }

    private async Task QueueHostOrder(IPublishEndpoint context, HostOrder hostOrder, DateTime creationDate)
    {
        // Short circuit
        var actionGroup = hostOrder.TryCreatePickDropActionGroup(configuration, creationDate);
        if (actionGroup != null)
        {
            bool picksAt1020 = hostOrder.Actions.First(a => a.ActionType == ActionType.Pick).Node == configuration.PrismatAklPickTarget;
            // Check if there are agvs that are to be selected to handle this order directly after dropping
            if (await hostOrderRepository.HasDropOrdersHeadingTowardsShortLoopTriggerPoint() && picksAt1020)
            {
                return;
            }
            await context.Publish(new QueueActionGroup(actionGroup, false));
            hostOrder.IsQueued = true;
            await hostOrderRepository.SaveAsync();
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
            actionGroup = hostOrder.CreateMovePickDropActionGroup(configuration, creationDate);
            await context.Publish(new QueueActionGroup(actionGroup, false));
            hostOrder.IsQueued = true;
            await hostOrderRepository.SaveAsync();
            return;
        }

        if (hostOrder.HasUnknownDropTarget(configuration))
        {
            // Check if there are agvs that are to be selected to handle this order directly after dropping
            if (await hostOrderRepository.HasDropOrdersHeadingTowardsCargoTriggerPoint())
            {
                return;
            }
            // Call for any idle agv to handle this order
            actionGroup = hostOrder.CreatePickMoveDropActionGroup(configuration);
            await context.Publish(new QueueActionGroup(actionGroup, false));
            hostOrder.IsQueued = true;
            await hostOrderRepository.SaveAsync();
        }
    }

    /// <summary>
    /// Handles the <see cref="CancelHostOrder"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<CancelHostOrder> context)
    {
        string hu = context.Message.Hu;
        var hostOrder = await hostOrderRepository.GetByHu(hu);
        if (hostOrder == null)
        {
            logger.LogError("Could not cancel HostOrder with HandlingUnit {Hu}; None exist.", hu);
            return;
        }
        hostOrder.Cancel(DateTime.UtcNow);
        await hostOrderRepository.SaveAsync();

        await context.Publish(new CancelOrAbortActionGroup(hostOrder.CorrelationId));

        HostOrder? reverseHostOrder = null;
        if (hostOrder.Actions.Any(a => a.ActionType == ActionType.Pick && configuration.PrismatKnownTargets.Contains(a.Node)))
        {
            reverseHostOrder = await hostOrderRepository.GetNextNotQueuedHostOrderPickingAt(configuration.PrismatAklPickTarget);
        }

        if (hostOrder.HasUnknownDropTarget(configuration))
        {
            reverseHostOrder = await hostOrderRepository.GetNextNotQueuedHostOrderPickingAt(null);
        }

        if (hostOrder.HasUnknownPickTarget(configuration))
        {
            reverseHostOrder = await hostOrderRepository.GetNextNotQueuedHostOrderPickingAt(null, true);
        }

        if (reverseHostOrder != null)
        {
            await QueueHostOrder(context, reverseHostOrder, reverseHostOrder.CreatedDate);
        }
    }

    /// <summary>
    /// Generates pick information for a host order.
    /// </summary>
    /// <param name="createHostOrderMessage">The message to generate cargo rotation information from.</param>
    /// <param name="configuration">The configuration with lift height information.</param>
    /// <returns></returns>
    private static (Dictionary<InfoToAgvKey, string>?, Dictionary<InfoToAgvKey, string>?) GeneratePickAndDropInformation(CreateHostOrder createHostOrderMessage, KaeserGraphConfiguration configuration)
    {
        Dictionary<InfoToAgvKey, string>? pickInfoToAgv = null;
        Dictionary<InfoToAgvKey, string>? dropInfoToAgv = null;

        var orderDetails = createHostOrderMessage.OrderDetails;
        
        double? liftHeightPick = configuration.GetLiftHeight(orderDetails.Source);
        double? liftHeightDrop = configuration.GetLiftHeight(orderDetails.Destination);

        if (orderDetails.LeRotation != "" || liftHeightPick.HasValue)
        {
            pickInfoToAgv = new Dictionary<InfoToAgvKey, string>();
            if (orderDetails.LeRotation != "")
            {
                pickInfoToAgv.Add(InfoToAgvKey.CargoRotation, orderDetails.LeRotation);
            }
            if (liftHeightPick.HasValue)
            {
                pickInfoToAgv.Add(InfoToAgvKey.LiftHeight, liftHeightPick.ToString()!);
            }
        }

        if (liftHeightDrop.HasValue)
        {
            dropInfoToAgv = new Dictionary<InfoToAgvKey, string> { { InfoToAgvKey.LiftHeight, liftHeightDrop.ToString()! } };
        }

        return (pickInfoToAgv, dropInfoToAgv);
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
        (var correlationId, Guid? assignedAgv, string targetNodeId, bool shouldNotPickNext) = context.Message;
        var hostOrder = await hostOrderRepository.GetByIdAsync(correlationId);
       
        if (hostOrder.HasUnknownDropTarget(configuration))
        {   // only trigger dual operation, if it is a drop order
            hostOrder.PassTriggerPoint();
            await hostOrderRepository.UpdateAsync(hostOrder);
            await context.Publish(new DecisionPointPassed(assignedAgv!.Value, targetNodeId, shouldNotPickNext));
        }
    }

    /// <summary>
    /// Handles the <see cref="QueuePickDropActionGroupForAgv"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<QueuePickDropActionGroupForAgv> context)
    {
        (var correlationId, var assignedAgv, string pickNodeId, bool shouldNotPickNext) = context.Message;
        var hostOrder = await hostOrderRepository.GetByIdAsync(correlationId);
        var actionGroup = hostOrder.CreatePickDropActionGroupForAgv(pickNodeId, shouldNotPickNext? null: assignedAgv, configuration);
        await hostOrderRepository.UpdateAsync(hostOrder);
        await context.Publish(new QueueActionGroup(actionGroup, false));
        hostOrder.IsQueued = true;
        await hostOrderRepository.SaveAsync();
    }

    /// <summary>
    /// Handles the <see cref="PassCargoTriggerPoint"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<PassCargoTriggerPoint> context)
    {
        var (assignedAgv, correlationId) = context.Message;
        try
        {
            var hostOrder = await hostOrderRepository.GetByIdAsync(correlationId);
            if (hostOrder.HasUnknownPickTarget(configuration))
            {
                hostOrder.PassTriggerPoint();
                await hostOrderRepository.UpdateAsync(hostOrder);
                await context.Publish(new CargoTriggerPointPassed(assignedAgv));
            }
        }
        catch (InvalidOperationException)
        {
            // not id of host order
        }
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
        hostOrder.IsQueued = true;
        await hostOrderRepository.SaveAsync();
    }

    /// <summary>
    /// Handles the <see cref="FinishPick"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<FinishPick> context)
    {
        var (correlationId, actionCorrelationId, timestamp) = context.Message;
        var hostOrder = await hostOrderRepository.GetByIdAsync(correlationId);
        var pickAction = hostOrder.Actions.SingleOrDefault(a => a.ActionType == ActionType.Pick && a.CorrelationId == actionCorrelationId);
        if (pickAction == null)
        {
            logger.LogError("Could not report completion off pick action {ActionCorrelationId} of group {CorrelationId}, action not found ", actionCorrelationId, correlationId);
            return;
        }
        hostCommunicator.ReportPickCompletion(hostOrder.Hu, pickAction.Node, timestamp);
    }

    /// <summary>
    /// Handles the <see cref="PassShortLoopTriggerPoint"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<PassShortLoopTriggerPoint> context)
    {
        (var assignedAgv, var correlationId, bool shouldNotPickNext) = context.Message;
        var hostOrder = await hostOrderRepository.GetByIdAsync(correlationId);
        bool dropsAt1050 = hostOrder.Actions.First(a => a.ActionType == ActionType.Pick).Node != configuration.PrismatAklPickTarget;
        if (dropsAt1050)
        {
            hostOrder.PassTriggerPoint();
            await hostOrderRepository.UpdateAsync(hostOrder);
            await context.Publish(new ShortLoopTriggerPointPassed(assignedAgv, shouldNotPickNext));
        }
    }
}