using System.Globalization;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using Gebhardt.Acc.PrismatCommunication.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Domain.Services;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands;
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
    IConsumer<FinishPick>,
    IConsumer<PassTriggerPoint>

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
        logger.LogInformation("Host order for order {Hu} created", hu);

        string doppelSpielDropTarget = configuration.GetDoppelspielDropTargetForPick(hostOrder.Actions.First(a => a.ActionType == ActionType.Pick).Node);
       
        // Check if there are agvs that are to be selected to handle this order directly after dropping
        if (await hostOrderRepository.HasDropOrdersHeadingTowardsDoppelspielTriggerPoint(doppelSpielDropTarget) && !configuration.SkipDoppelspiel(doppelSpielDropTarget) )
        {
            return;
        }

        // Call for any idle agv to handle this order
        await QueueHostOrder(context, hostOrder, orderDetails.CreationDate);
    }

    private async Task QueueHostOrder(IPublishEndpoint context, HostOrder hostOrder, DateTime creationDate, Guid? assignedAgv = null)
    {
        // Short circuit
        var actionGroup = hostOrder.TryCreatePickDropActionGroup(configuration, creationDate);
        if (actionGroup != null)
        {
            actionGroup.AssignedAgv = assignedAgv;
            await context.Publish(new QueueActionGroup(actionGroup, false));
            hostOrder.IsQueued = true;
            await hostOrderRepository.SaveAsync();
            return;
        }

        // Long circuit
        if (hostOrder.HasUnknownPickTarget(configuration))
        {
            actionGroup = hostOrder.CreateMovePickDropActionGroup(configuration, creationDate);
            actionGroup.AssignedAgv = assignedAgv;
            await context.Publish(new QueueActionGroup(actionGroup, false));
            hostOrder.IsQueued = true;
            await hostOrderRepository.SaveAsync();
            return;
        }

        if (hostOrder.HasUnknownDropTarget(configuration))
        {
            actionGroup = hostOrder.CreatePickMoveDropActionGroup(configuration);
            actionGroup.AssignedAgv = assignedAgv;
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
            reverseHostOrder = await hostOrderRepository.GetNextNotQueuedHostOrderPickingAt(null, false);
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

        if (Math.Abs(orderDetails.LeRotation) > Mathematics.DoubleComparator || liftHeightPick.HasValue)
        {
            pickInfoToAgv = new Dictionary<InfoToAgvKey, string>();
            if (Math.Abs(orderDetails.LeRotation) > Mathematics.DoubleComparator)
            {
                pickInfoToAgv.Add(InfoToAgvKey.CargoRotation, orderDetails.LeRotation.ToString(CultureInfo.InvariantCulture));
            }
            if (liftHeightPick.HasValue)
            {
                pickInfoToAgv.Add(InfoToAgvKey.LiftHeight, liftHeightPick.Value.ToString(CultureInfo.InvariantCulture));
            }
        }

        if (liftHeightDrop.HasValue)
        {
            dropInfoToAgv = new Dictionary<InfoToAgvKey, string> { { InfoToAgvKey.LiftHeight, liftHeightDrop.Value.ToString(CultureInfo.InvariantCulture) } };
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
    /// Handles the <see cref="PassTriggerPoint"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<PassTriggerPoint> context)
    {
        (var agvCorrelationId, var droppingHostOrderCorrelationId, Guid? nextPickHostOrderCorrelationId, bool shouldPickNext) = context.Message;
        var droppingHostOrder = await hostOrderRepository.GetByIdAsync(droppingHostOrderCorrelationId);
        droppingHostOrder.PassTriggerPoint();
        await hostOrderRepository.UpdateAsync(droppingHostOrder);
        await hostOrderRepository.SaveAsync();
       
        if (shouldPickNext)
        {
            var pickingHostOrder = await hostOrderRepository.GetByIdAsync(nextPickHostOrderCorrelationId!.Value);
            await QueueHostOrder(context, pickingHostOrder, DateTime.UtcNow, agvCorrelationId);
        }
    }
}