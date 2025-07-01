using System.Globalization;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.ActionGroups.Requests;
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
public class HostOrderCommandHandler(
    ILogger<HostOrderCommandHandler> logger,
    IHostOrderRepository hostOrderRepository,
    IHostCommunicator hostCommunicator,
    KaeserGraphConfiguration configuration,
    IHostOrderValidationService validationService,
    ISystemStatusRepository systemStatusRepository,
    IStationRepository stationRepository) :
    IConsumer<CreateHostOrder>,
    IConsumer<CancelHostOrder>,
    IConsumer<CancelInvalidHostOrder>,
    IConsumer<HandleHostOrderStart>,
    IConsumer<FinishHostOrder>,
    IConsumer<FinishPick>,
    IConsumer<PassTriggerPoint>,
    IConsumer<AbortHostOrder>,
    IConsumer<TryQueueDelayedOrder>,
    IConsumer<ResetDoppelspielPartner>,
    IConsumer<TryQueueNextAklPick>,
    IConsumer<TriggerDoppelspielpartnerQueueing>

{
    /// <summary>
    /// Handles the <see cref="CreateHostOrder"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<CreateHostOrder> context)
    {
        var orderDetails = context.Message.OrderDetails;
        string hu = context.Message.Hu;

        var hostOrder = new HostOrder(orderDetails.OrderId, hu, orderDetails.CreationDate);

        (Dictionary<InfoToAgvKey, string>? pickInfoToAgv, Dictionary<InfoToAgvKey, string>? dropInfoToAgv) = GeneratePickAndDropInformation(context.Message);
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

        if (configuration.IsPickAtAkl(hostOrder.Source))
        {
            logger.LogDebug("Order {OrderCorrelationId}, is pick at AKL: delay queueing it", hostOrder.CorrelationId);
            await CreateHostOrder(context, hostOrder);
            return;
        }

        // Check if there are agvs that are to be selected to handle this order directly after dropping
        string doppelSpielDropTarget = configuration.GetDoppelspielDropTargetForPick(hostOrder.Source);
        logger.LogInformation("Host order for order {Hu} created: {OrderCorrelationId}, Doppelspiel drop target corresponding to current pick {Pick} : {Target}", hu, hostOrder.CorrelationId, hostOrder.Source, doppelSpielDropTarget);

        Guid? dropOrderId = await hostOrderRepository.GetNextDropOrderHeadingTowardsDoppelspielTriggerPoint(doppelSpielDropTarget);
        if (dropOrderId != null && !configuration.SkipDoppelspiel(doppelSpielDropTarget))
        {
            var dropOrder = await hostOrderRepository.GetByIdAsync(dropOrderId.Value);
            dropOrder.RegisterForDoppelspiel();
            hostOrder.RegisterForDoppelspiel();
            await hostOrderRepository.UpdateAsync(dropOrder);
            await hostOrderRepository.UpdateAsync(hostOrder);
            await hostOrderRepository.SaveAsync();
            logger.LogInformation("Doppelspiel expected for order {OrderCorrelationId}", hostOrder.CorrelationId);
            await CreateHostOrder(context, hostOrder, dropOrder.AssignedAgv);
            return;
        }

        if (await ViolatesAklAgvLimitation(hostOrder))
        {
            await CreateHostOrder(context, hostOrder, hostOrder.AssignedAgv);
            return;
        }

        // Call for any idle agv to handle this order
        await QueueHostOrder(context, hostOrder);
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

        HostOrder? unmatchedPick = null;
        if (hostOrder.IsRegisteredForDoppelspiel)
        {
            List<string>? sources = configuration.DoppelspielPickTargets.GetValueOrDefault(hostOrder.Target);
            if (sources != null)
            {
                unmatchedPick = await hostOrderRepository.GetNextUnmatchedDoppelspielPick(sources, [hostOrder.Target], hostOrder.CorrelationId);
            }
        }

        if (unmatchedPick is { IsQueued: false })
        {
            await QueueHostOrder(context, unmatchedPick);
            unmatchedPick.UnRegisterFromDoppelspiel();
        }
    }

    private async Task CreateHostOrder(IPublishEndpoint context, HostOrder hostOrder, Guid? assignedAgv = null)
    {
        var actionGroup = hostOrder.CreateActionGroup(configuration, assignedAgv);
        await context.Publish(new CreateActionGroup(actionGroup));
        hostOrder.IsQueued = false;
        await hostOrderRepository.SaveAsync();
    }

    private async Task<bool> QueueHostOrder(IPublishEndpoint context, HostOrder hostOrder, Guid? assignedAgv = null, string? pickNode = null)
    {
        if (!configuration.IsPickAtAkl(hostOrder.Source) && await ViolatesAklAgvLimitation(hostOrder))
        {
            logger.LogInformation("Host order {OrderId} / {CorrelationId} is kept back because of AKL station limitation.", hostOrder.Hu, hostOrder.CorrelationId);
            return false;
        }

        var actionGroup = hostOrder.CreateActionGroup(configuration, assignedAgv, pickNode);
        await context.Publish(new QueueActionGroup(actionGroup, false));
        hostOrder.IsQueued = true;
        await hostOrderRepository.SaveAsync();
        return true;
    }

    private async Task<bool> ViolatesAklAgvLimitation(HostOrder hostOrder)
    {
        if (!hostOrder.IsShortCircuitOrder(configuration))
        {
            // Long loop order
            return false;
        }

        if (configuration.PrismatInputLanes.Contains(hostOrder.Source) && hostOrder.IsRegisteredForDoppelspiel)
        {
            // Already reported Doppelspiel partner
            return false;
        }

        var systemStatus = await systemStatusRepository.GetStatus();
        if (systemStatus is not { UseAklAgvLimit: true })
        {
            return false;
        }

        var stationGroup = await stationRepository.GetStationGroup(hostOrder.Source, hostOrder.Target);
        if (stationGroup == null)
        {
            return false;
        }

        if (stationGroup.IsOccupied)
        {
            logger.LogWarning("Delaying host order {CorrelationId}, station group {StationGroupId} at {FirstSource}/{LastSource} is occupied.", hostOrder.CorrelationId, stationGroup.GroupId, stationGroup.Sources.First(), stationGroup.Sources.Last());
            return true;
        }

        if (await hostOrderRepository.HasCurrentHostOrdersForStationGroup(stationGroup.Sources, stationGroup.Targets))
        {
            logger.LogWarning("Delaying host order {CorrelationId}, orders are heading towards it.", hostOrder.CorrelationId);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Handles the <see cref="CancelHostOrder"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<CancelHostOrder> context)
    {
        HostOrder? hostOrder = null;
        string hu = context.Message.Hu;
        foreach (int _ in new int[2])
        {
            // Wait 10s to ensure host order is saved in db
            // in case cancellation is requested right after creation
            await Task.Delay(TimeSpan.FromSeconds(10));
            hostOrder = await hostOrderRepository.GetLatestUnfinishedOrderByHu(hu);
            if (hostOrder != null)
            {
                break;
            }
        }

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
    /// <returns></returns>
    private static (Dictionary<InfoToAgvKey, string>?, Dictionary<InfoToAgvKey, string>?) GeneratePickAndDropInformation(CreateHostOrder createHostOrderMessage)
    {
        Dictionary<InfoToAgvKey, string>? pickInfoToAgv = null;
        Dictionary<InfoToAgvKey, string>? dropInfoToAgv = null;

        var orderDetails = createHostOrderMessage.OrderDetails;

        if (Math.Abs(orderDetails.LeRotation) > Mathematics.DoubleComparator )
        {
            pickInfoToAgv = new Dictionary<InfoToAgvKey, string>();
            if (Math.Abs(orderDetails.LeRotation) > Mathematics.DoubleComparator)
            {
                pickInfoToAgv.Add(InfoToAgvKey.CargoRotation, orderDetails.LeRotation.ToString(CultureInfo.InvariantCulture));
            }
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
        (var correlationId, var timestamp, string? _) = context.Message;
        var hostOrder = await hostOrderRepository.GetByIdAsync(correlationId);
        hostOrder.Finish(timestamp);
        await hostOrderRepository.UpdateAsync(hostOrder);
        await hostCommunicator.ReportOrderCompletion(hostOrder.Hu, hostOrder.OrderId, hostOrder.Actions.OrderBy(a => a.SequenceNumber).Last().Node);
        logger.LogInformation("Finished host order {HostOrder} with DoppelSpielPartner {Partner}", hostOrder.CorrelationId, hostOrder.DoppelspielPartner);
    }

    /// <summary>
    /// Handles the <see cref="FinishPick"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<FinishPick> context)
    {
        var (correlationId, actionCorrelationId, timestamp) = context.Message;
        var hostOrder = await hostOrderRepository.GetByIdAsync(correlationId);
        hostOrder.FinishAction(timestamp, ActionType.Pick);
        var pickAction = hostOrder.Actions.SingleOrDefault(a => a.ActionType == ActionType.Pick);
        var action = hostOrder.Actions.SingleOrDefault(a => a.CorrelationId == actionCorrelationId);
        if (pickAction == null || (action != null && action.ActionType != ActionType.Pick))
        {
            logger.LogError("Could not report completion off pick action {ActionCorrelationId} of group {CorrelationId}, action not found ", actionCorrelationId, correlationId);
            return;
        }
        hostCommunicator.ReportPickCompletion(hostOrder.Hu, pickAction.Node, timestamp);
        await hostOrderRepository.UpdateAsync(hostOrder);
    }

    /// <summary>
    /// Handles the <see cref="PassTriggerPoint"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<PassTriggerPoint> context)
    {
        (var agvCorrelationId, var droppingHostOrderCorrelationId, Guid? nextPickHostOrderCorrelationId, bool shouldPickNext, string? pickNode) = context.Message;
        var droppingHostOrder = await hostOrderRepository.GetByIdAsync(droppingHostOrderCorrelationId);
        droppingHostOrder.PassTriggerPoint();
       
        if (shouldPickNext)
        {
            var pickingHostOrder = await hostOrderRepository.GetByIdAsync(nextPickHostOrderCorrelationId!.Value);
            pickingHostOrder.SetDoppelspielpartner(droppingHostOrderCorrelationId);
            logger.LogInformation("Dropping agv {AgvCorrelationId} should pick next: Queueing order {HostOrder} with pick node {Node}", agvCorrelationId, pickingHostOrder.CorrelationId, pickingHostOrder.HasUnknownDropTarget(configuration) ? null : pickNode);
            if (await QueueHostOrder(context, pickingHostOrder, agvCorrelationId, pickingHostOrder.HasUnknownDropTarget(configuration) ? null : pickNode))
            {
                droppingHostOrder.SetDoppelspielpartner(pickingHostOrder.CorrelationId);
                await hostOrderRepository.UpdateAsync(droppingHostOrder);
                await hostOrderRepository.UpdateAsync(pickingHostOrder);
                await hostOrderRepository.SaveAsync();
                return;
            }
        }

        if (droppingHostOrder.IsRegisteredForDoppelspiel)
        {
            await HandleRelatedDoppelspielOrders(context, droppingHostOrder);
            droppingHostOrder.UnRegisterFromDoppelspiel();
        }
        await hostOrderRepository.UpdateAsync(droppingHostOrder);
        await hostOrderRepository.SaveAsync();
    }

    /// <summary>
    /// Handles the <see cref="AbortHostOrder"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<AbortHostOrder> context)
    {
        var correlationId = context.Message.CorrelationId;
        try
        {
            var hostOrder = await hostOrderRepository.GetByIdAsync(correlationId);
            hostOrder.Cancel(DateTime.UtcNow);
            if (hostOrder is { IsRegisteredForDoppelspiel: true, HasQueuedDoppelspielpartner: false })
            {
                await HandleRelatedDoppelspielOrders(context, hostOrder);
                hostOrder.UnRegisterFromDoppelspiel();
            }
            await hostOrderRepository.UpdateAsync(hostOrder);
            await hostOrderRepository.SaveAsync();
        }
        catch (InvalidOperationException)
        {
            // not order known to prismat
        }
    }
    
    /// <summary>
    /// Handles the <see cref="TryQueueDelayedOrder"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<TryQueueDelayedOrder> context)
    {
        var stationGroup = await stationRepository.GetStationGroup(context.Message.Source, context.Message.Target);
        if (stationGroup == null)
        {
            logger.LogInformation("Station group AKL changed. Failed to queue next order because there is no station group with source {Source} and target {Target}", context.Message.Source, context.Message.Target);
            return;
        }

        ICollection<string> sources = stationGroup.Sources;
        if (configuration.IsPickAtAkl(context.Message.Source))
        {  
            // Do not queue AKL picks here, they are queued upon finish of the pick of a drop at AKL
            sources = [];
        }

        var hostOrder = await hostOrderRepository.GetNextHostOrderForStationGroup(sources, stationGroup.Targets);
        if (hostOrder == null)
        {
            return;
        }

        logger.LogInformation("Station group AKL changed. Queuing order {OrderId} / {CorrelationId} that was kept back because of AKL station limitation.", hostOrder.Hu, hostOrder.CorrelationId);
        await QueueHostOrder(context, hostOrder);
    }

    /// <summary>
    /// Handles the <see cref="ResetDoppelspielPartner"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<ResetDoppelspielPartner> context)
    {
        var actionGroupId = context.Message.ActionGroupCorrelationId;
        
        var hostOrder = await hostOrderRepository.GetByIdAsync(actionGroupId);

        if (hostOrder is { IsRegisteredForDoppelspiel: true, HasQueuedDoppelspielpartner: false })
        {
            hostOrder.UnRegisterFromDoppelspiel();
            await hostOrderRepository.UpdateAsync(hostOrder);
        }
    }

    /// <summary>
    /// Handles the <see cref="TriggerDoppelspielpartnerQueueing"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<TriggerDoppelspielpartnerQueueing> context)
    {
        var (orderId, agvCorrelationId, pickNode) = context.Message;
        var hostOrder = await hostOrderRepository.GetByIdAsync(orderId);
        if (hostOrder.HasQueuedDoppelspielpartner) // has already queued its Doppelspielpartner
        {
            await context.RespondAsync(new DoppelspielPartnerQueued(null));
            return;
        }
        List<string>? sources = configuration.DoppelspielPickTargets.GetValueOrDefault(hostOrder.Target);
        if (sources == null) // is not at station supposed to do late Doppelspiel
        {
            await context.RespondAsync(new DoppelspielPartnerQueued(null));
            return;
        }
       
        var pickingHostOrder = await hostOrderRepository.GetNextPickForSources(sources);
        if (pickingHostOrder == null)
        {
            await context.RespondAsync(new DoppelspielPartnerQueued(null));
            return;
        }

        logger.LogInformation("Try queueing order {HostOrder} with pick node {Node}", pickingHostOrder.CorrelationId, pickingHostOrder.HasUnknownDropTarget(configuration) ? null : pickNode);
        if (await QueueHostOrder(context, pickingHostOrder, agvCorrelationId, pickingHostOrder.HasUnknownDropTarget(configuration) ? null : pickNode))
        {
            pickingHostOrder.RegisterForDoppelspiel();
            pickingHostOrder.SetDoppelspielpartner(hostOrder.CorrelationId);
            hostOrder.RegisterForDoppelspiel();
            hostOrder.SetDoppelspielpartner(pickingHostOrder.CorrelationId);

            await hostOrderRepository.UpdateAsync(pickingHostOrder);
            await hostOrderRepository.UpdateAsync(hostOrder);
            await hostOrderRepository.SaveAsync();
            await context.RespondAsync(new DoppelspielPartnerQueued(pickingHostOrder.CorrelationId));
            return;
        }
        await context.RespondAsync(new DoppelspielPartnerQueued(null));
    }

    /// <summary>
    /// Handles the <see cref="TryQueueNextAklPick"/> command
    /// </summary>
    public async Task Consume(ConsumeContext<TryQueueNextAklPick> context)
    {
        (string pickTarget, var agvCorrelationId) = context.Message;
        var hostOrder = await hostOrderRepository.GetNextHostOrderForStationGroup([pickTarget], []);
        if (hostOrder == null)
        {
            logger.LogInformation("No delayed next AKL pick at {Target}", pickTarget);
            return;
        }

        logger.LogInformation("Queueing next AKL pick order {CorrelationId} at {Target} for agv {AgvCorrelationId}", hostOrder.CorrelationId, pickTarget, agvCorrelationId);
        await QueueHostOrder(context, hostOrder, agvCorrelationId);
    }
}