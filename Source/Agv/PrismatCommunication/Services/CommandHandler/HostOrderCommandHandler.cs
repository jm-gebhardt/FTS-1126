using System.Globalization;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using Gebhardt.Acc.PrismatCommunication.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Domain.Services;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands.HostOrder;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.PrismatCommunication.Services.CommandHandler;

/// <summary>
/// Provides handlers for host order related commands.
/// </summary>
[UsedImplicitly]
public class HostOrderCommandHandler(ILogger<HostOrderCommandHandler> logger, IHostOrderRepository hostOrderRepository, IHostCommunicator hostCommunicator, KaeserGraphConfiguration configuration, IHostOrderValidationService validationService) :
    IConsumer<CreateHostOrder>,
    IConsumer<CancelHostOrder>,
    IConsumer<CancelInvalidHostOrder>,
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

        var hostOrder = new HostOrder(orderDetails.OrderId, hu, orderDetails.CreationDate);

        (Dictionary<InfoToAgvKey, string>? pickInfoToAgv, Dictionary<InfoToAgvKey, string>? dropInfoToAgv) = GeneratePickAndDropInformation(context.Message, configuration);
        logger.LogDebug("pickInfoToAgv: {PickInfoToAgv}", pickInfoToAgv == null ? "empty" : string.Join(", ", pickInfoToAgv.Select(kvp => $"{kvp.Key}: {kvp.Value}")));
        logger.LogDebug("dropInfoToAgv: {DropInfoToAgv}", dropInfoToAgv == null ? "empty" : string.Join(", ", dropInfoToAgv.Select(kvp => $"{kvp.Key}: {kvp.Value}")));

        hostOrder.AddAction(orderDetails.Source, ActionType.Pick, pickInfoToAgv);
        hostOrder.AddAction(orderDetails.Destination, ActionType.Drop, dropInfoToAgv);

        (int ErrorCode, string ErrorInfo)? validationError = await validationService.ValidateAsync(hostOrder);
        if (validationError != null)
        {
            hostCommunicator.ReportOrderError(hostOrder.Hu, orderDetails.Source, orderDetails.Destination, validationError.Value.ErrorCode, validationError.Value.ErrorInfo, DateTime.UtcNow);
            hostOrder.Cancel(DateTime.UtcNow);
        }
        hostOrderRepository.Add(hostOrder);
        await hostOrderRepository.SaveAsync();
        if (hostOrder.CanceledDate != null)
        {
            return;
        }
        await CancelUnfinishedOrdersOfHu(context, hu, orderDetails.CreationDate);

        // Check if there are agvs that are to be selected to handle this order directly after dropping
        string doppelSpielDropTarget = configuration.GetDoppelspielDropTargetForPick(hostOrder.Actions.First(a => a.ActionType == ActionType.Pick).Node);
        logger.LogInformation("Host order for order {Hu} created: {OrderCorrelationId}, corresponding drop target {Target}", hu, hostOrder.CorrelationId, doppelSpielDropTarget);

        Guid? dropOrderId = await hostOrderRepository.GetNextDropOrderHeadingTowardsDoppelspielTriggerPoint(doppelSpielDropTarget);
        if (dropOrderId != null && !configuration.SkipDoppelspiel(doppelSpielDropTarget))
        {
            hostOrder.SetDoppelspielPartner(dropOrderId.Value);
            await hostOrderRepository.UpdateAsync(hostOrder);
            var dropOrder = await hostOrderRepository.GetByIdAsync(dropOrderId.Value);
            dropOrder.SetDoppelspielPartner(hostOrder.CorrelationId);
            await hostOrderRepository.UpdateAsync(dropOrder);
            await hostOrderRepository.SaveAsync();
            logger.LogInformation("Doppelspiel expected for order  {OrderCorrelationId}", hostOrder.CorrelationId);
            return;
        }

        // Call for any idle agv to handle this order
        await QueueHostOrder(context, hostOrder, orderDetails.CreationDate);
    }

    private async Task CancelUnfinishedOrdersOfHu(IPublishEndpoint context, string hu, DateTime creationDate)
    {
        ICollection<HostOrder> hostOrders = await hostOrderRepository.GetLatestUnfinishedOrdersByHu(hu, creationDate);
        if (hostOrders.Count == 0)
        {
            logger.LogDebug("No unfinished host orders for HU {hu}", hu);
            return;
        }
        foreach (var hostOrder in hostOrders)
        {
            hostOrder.Cancel(DateTime.UtcNow);

            await context.Publish(new CancelOrAbortActionGroup(hostOrder.CorrelationId));

            await HandleRelatedDoppelspielOrders(context, hostOrder);
        }
        await hostOrderRepository.SaveAsync();
    }

    private async Task HandleRelatedDoppelspielOrders(IPublishEndpoint context, HostOrder hostOrder)
    {
        // Handle related Doppelspiel orders

        HostOrder? reverseHostOrder = null;
        if (hostOrder.DoppelspielPartner != null)
        {
            reverseHostOrder = await hostOrderRepository.GetByIdAsync(hostOrder.DoppelspielPartner.Value);
        }

        if (reverseHostOrder is { IsQueued: false })
        {
            reverseHostOrder.SetDoppelspielPartner(null);
            await hostOrderRepository.UpdateAsync(reverseHostOrder);
            await hostOrderRepository.SaveAsync();
            await QueueHostOrder(context, reverseHostOrder, reverseHostOrder.CreatedDate);
        }
    }

    private async Task QueueHostOrder(IPublishEndpoint context, HostOrder hostOrder, DateTime creationDate, Guid? assignedAgv = null, string? pickNode = null)
    {
        ActionGroup? actionGroup;
        if (pickNode != null && assignedAgv != null)
        {
            // Doppelspiel
            actionGroup = hostOrder.CreatePickDropActionGroupForAgv(pickNode, assignedAgv, configuration);
            await context.Publish(new QueueActionGroup(actionGroup, false));
            hostOrder.IsQueued = true;
            await hostOrderRepository.SaveAsync();
            return;
        }

        // Short circuit
        actionGroup = hostOrder.TryCreatePickDropActionGroup(configuration, creationDate);
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
        // Wait 10s to ensure host order is saved in db
        // in case cancellation is requested right after creation
        await Task.Delay(10);
        string hu = context.Message.Hu;
        var hostOrder = await hostOrderRepository.GetLatestUnfinishedOrderByHu(hu);
        if (hostOrder == null)
        {
            logger.LogError("Could not cancel HostOrder with HandlingUnit {Hu}; None exist.", hu);
            return;
        }
        hostOrder.Cancel(DateTime.UtcNow);
        await hostOrderRepository.SaveAsync();

        await context.Publish(new CancelOrAbortActionGroup(hostOrder.CorrelationId));

        await HandleRelatedDoppelspielOrders(context, hostOrder);
    }

    /// <summary>
    /// Handles the <see cref="CancelInvalidHostOrder"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<CancelInvalidHostOrder> context)
    {
        var hostOrder = await hostOrderRepository.GetByIdAsync(context.Message.CorrelationId);
        hostOrder.Cancel(DateTime.UtcNow);
        await hostOrderRepository.SaveAsync();

        hostCommunicator.ReportOrderError(hostOrder.Hu,
            hostOrder.Actions.First(a => a.ActionType == ActionType.Pick).Node,
            hostOrder.Actions.First(a => a.ActionType == ActionType.Drop).Node,
            KaeserGraphConfiguration.ErrorCodeNoRoute,
            configuration.OrderValidationErrors[KaeserGraphConfiguration.ErrorCodeNoRoute],
            DateTime.UtcNow);

        await HandleRelatedDoppelspielOrders(context, hostOrder);
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
        await hostCommunicator.ReportOrderCompletion(hostOrder.Hu, hostOrder.OrderId, hostOrder.Actions.OrderBy(a => a.SequenceNumber).Last().Node);
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
        (var agvCorrelationId, var droppingHostOrderCorrelationId, Guid? nextPickHostOrderCorrelationId, bool shouldPickNext, string? pickNode) = context.Message;
        var droppingHostOrder = await hostOrderRepository.GetByIdAsync(droppingHostOrderCorrelationId);
        droppingHostOrder.PassTriggerPoint();
        await hostOrderRepository.UpdateAsync(droppingHostOrder);
        await hostOrderRepository.SaveAsync();

        if (shouldPickNext)
        {
            var pickingHostOrder = await hostOrderRepository.GetByIdAsync(nextPickHostOrderCorrelationId!.Value);
            if (pickingHostOrder.DoppelspielPartner != droppingHostOrder.CorrelationId)
            {
                logger.LogError("Mismatch in Doppelspiel partner, expected {ExpectedPartner}, got {ActualPartner}, calling for different agv", pickingHostOrder.DoppelspielPartner, droppingHostOrder.CorrelationId);
                await QueueHostOrder(context, pickingHostOrder, DateTime.UtcNow);
                return;
            }
            logger.LogInformation("Queueing order {HostOrder} with pick node {Node}", pickingHostOrder.CorrelationId, pickingHostOrder.HasUnknownDropTarget(configuration) ? null : pickNode);
            await QueueHostOrder(context, pickingHostOrder, DateTime.UtcNow, agvCorrelationId, pickingHostOrder.HasUnknownDropTarget(configuration) ? null : pickNode);
        }
    }
}