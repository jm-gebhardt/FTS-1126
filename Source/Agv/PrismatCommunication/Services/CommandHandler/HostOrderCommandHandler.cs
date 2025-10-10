using System.Globalization;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.ActionGroups.Requests;
using Gebhardt.Acc.Common.Messages.Queue;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using Gebhardt.Acc.PrismatCommunication.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Domain.Semaphores;
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
    IStationRepository stationRepository,
    ISemaphoreProvider<IDoppelspielSemaphore> doppelspielSemaphore,
    IAklAgvLimitGuard aklAgvLimitGuard,
    IRequestClient<GetQueueFillQuantities> queueRequestClient
) :
    IConsumer<CreateHostOrder>,
    IConsumer<CancelHostOrder>,
    IConsumer<CancelInvalidHostOrder>,
    IConsumer<HandleHostOrderStart>,
    IConsumer<FinishHostOrder>,
    IConsumer<FinishPick>,
    IConsumer<TryQueueDoppelspielPartner>,
    IConsumer<AbortHostOrder>,
    IConsumer<TryQueueDelayedOrder>,
    IConsumer<ResetDoppelspielPartner>,
    IConsumer<TryQueueNextAklPick>,
    IConsumer<TriggerDoppelspielPartnerQueueing>,
    IConsumer<TryAssignDelayedOrderToAgv>,
    IConsumer<ConsiderForDoppelspiel>,
    IConsumer<QueueUnmatchedDoppelspielPicks>

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

        // Consider all orders for Doppelspiel right away that are not dropping at APL
        if (hostOrder.Drop?.Node != configuration.PrismatAplTarget)
        {
            hostOrder.ConsiderForDoppelspiel();
        }
        else
        {
            try
            {
                Response<GotQueueFillQuantities> queues = await queueRequestClient.GetResponse<GotQueueFillQuantities>(new GetQueueFillQuantities());
                if (queues.Message.FillQuantities.Count == 0 || queues.Message.FillQuantities.FirstOrDefault(fq => fq.Name == configuration.AplQueueName)?.EnteredAgvs == 0)
                {
                    hostOrder.ConsiderForDoppelspiel();
                }
            }
            catch
            {
                logger.LogWarning("No response on get queue fill quantities request");
            }
        }

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
        await doppelspielSemaphore.ExecuteLockedAsync(async () =>
        {
            string doppelSpielDropTarget = configuration.GetDoppelspielDropTargetForPick(hostOrder.Source);
            Guid? dropOrderId = await hostOrderRepository.GetNextDropOrderEligibleForDoppelspiel(doppelSpielDropTarget);
            if (dropOrderId != null)
            {
                var dropOrder = await hostOrderRepository.GetByIdAsync(dropOrderId.Value);
                dropOrder.RegisterDoppelspiel(ActionType.Drop);
                hostOrder.RegisterDoppelspiel(ActionType.Pick);
                await hostOrderRepository.UpdateAsync(dropOrder);
                await hostOrderRepository.UpdateAsync(hostOrder);
                await hostOrderRepository.SaveAsync();
                logger.LogInformation("Registered drop order {DropOrderCorrelationId} to potentially pick order {PickOrderCorrelationId} for Doppelspiel", dropOrder.CorrelationId, hostOrder.CorrelationId);
                await CreateHostOrder(context, hostOrder, dropOrder.AssignedAgv);
            }
        });

        if (hostOrder.Delay)
        {
            logger.LogInformation("Doppelspiel expected for order {OrderCorrelationId}, not queueing order", hostOrder.CorrelationId);
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
            logger.LogDebug("No unfinished host orders for HU {Hu}", hu);
            return;
        }
        foreach (var hostOrder in hostOrders)
        {
            hostOrder.Cancel(DateTime.UtcNow);

            await context.Publish(new CancelOrAbortActionGroup(hostOrder.CorrelationId));

            await QueueUnmatchedDoppelspielPicks(context, [hostOrder.Target], hostOrder.CorrelationId);
        }
        await hostOrderRepository.SaveAsync();
    }

    private async Task QueueUnmatchedDoppelspielPicks(IPublishEndpoint context, ICollection<string> targets, Guid? dropOrder = null)
    {
        await doppelspielSemaphore.ExecuteLockedAsync(async () =>
        {
            HostOrder? unmatchedPick = null;
            List<string>? sources = configuration.DoppelspielPickTargets.GetValueOrDefault(targets.First());
            if (sources != null)
            {
                unmatchedPick = await hostOrderRepository.GetNextUnmatchedDoppelspielPick(sources, targets, dropOrder);
            }

            if (unmatchedPick is { IsQueued: false })
            {
                await QueueHostOrder(context, unmatchedPick);
                unmatchedPick.UnregisterFromDoppelspiel(ActionType.Pick);
            }
        });
    }

    private async Task CreateHostOrder(IPublishEndpoint context, HostOrder hostOrder, Guid? assignedAgv = null)
    {
        var actionGroup = hostOrder.CreateActionGroup(configuration, assignedAgv);
        await context.Publish(new CreateActionGroup(actionGroup));
        hostOrder.IsQueued = false;
        await hostOrderRepository.SaveAsync();
    }

    private async Task<bool> QueueHostOrder(IPublishEndpoint context, HostOrder hostOrder, Guid? assignedAgv = null, GraphNode? pickNode = null)
    {
        if (!configuration.IsPickAtAkl(hostOrder.Source) && await ViolatesAklAgvLimitation(hostOrder))
        {
            logger.LogInformation("Host order {OrderId} / {CorrelationId} is kept back because of AKL station limitation", hostOrder.Hu, hostOrder.CorrelationId);
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

        if (configuration.PrismatInputLanes.Contains(hostOrder.Source) && hostOrder.IsRegisteredForDoppelspielPick)
        {
            // Already reported Doppelspiel partner
            return false;
        }

        return !await aklAgvLimitGuard.IsWithinAklAgvLimitAsync(hostOrder);
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
            logger.LogError("Could not cancel HostOrder with HandlingUnit {Hu}; None exist", hu);
            return;
        }
        hostOrder.Cancel(DateTime.UtcNow);
        await hostOrderRepository.SaveAsync();

        await context.Publish(new CancelOrAbortActionGroup(hostOrder.CorrelationId));
        if (!hostOrder.IsRegisteredForDoppelspielDrop)
        {
            return;
        }
        await QueueUnmatchedDoppelspielPicks(context, [hostOrder.Target], hostOrder.CorrelationId);
        await hostOrderRepository.SaveAsync();
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

        if (!hostOrder.IsRegisteredForDoppelspielDrop)
        {
            return;
        }
        await QueueUnmatchedDoppelspielPicks(context, [hostOrder.Target], hostOrder.CorrelationId);
        await hostOrderRepository.SaveAsync();
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

        if (Math.Abs(orderDetails.LeRotation) > Mathematics.DoubleComparator)
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
        var (correlationId, timestamp) = context.Message;
        var hostOrder = await hostOrderRepository.GetByIdAsync(correlationId);
        hostOrder.Finish(timestamp);
        await hostOrderRepository.UpdateAsync(hostOrder);
        await hostCommunicator.ReportOrderCompletion(hostOrder.Hu, hostOrder.OrderId, hostOrder.Actions.OrderBy(a => a.SequenceNumber).Last().Node);
        logger.LogInformation("Finished host order {HostOrder}, registered for drop Doppelspiel {IsRegisteredForDoppelspiel} DoppelSpielPartner {Partner}", hostOrder.CorrelationId, hostOrder.IsRegisteredForDoppelspielDrop, hostOrder.Actions.FirstOrDefault(a => a.ActionType == ActionType.Drop)?.DoppelspielPartner);
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
    /// Handles the <see cref="TryQueueDoppelspielPartner"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<TryQueueDoppelspielPartner> context)
    {
        (var agvCorrelationId, var droppingHostOrderCorrelationId, Guid? nextPickHostOrderCorrelationId, bool shouldPickNext, var pickNode) = context.Message;
        var droppingHostOrder = await hostOrderRepository.GetByIdAsync(droppingHostOrderCorrelationId);
        if (shouldPickNext && nextPickHostOrderCorrelationId != null)
        {
            var pickingHostOrder = await hostOrderRepository.GetByIdAsync(nextPickHostOrderCorrelationId.Value);
            pickingHostOrder.SetDoppelspielpartner(droppingHostOrderCorrelationId, ActionType.Pick);
            logger.LogInformation("Dropping agv {AgvCorrelationId} should pick next: Queueing order {HostOrder} with pick node {Node}", agvCorrelationId, pickingHostOrder.CorrelationId, pickingHostOrder.HasUnknownDropTarget(configuration) ? null : pickNode);
            if (await QueueHostOrder(context, pickingHostOrder, agvCorrelationId, pickingHostOrder.HasUnknownDropTarget(configuration) ? null : pickNode))
            {
                droppingHostOrder.SetDoppelspielpartner(pickingHostOrder.CorrelationId, ActionType.Drop);
                await hostOrderRepository.UpdateAsync(droppingHostOrder);
                await hostOrderRepository.UpdateAsync(pickingHostOrder);
                await hostOrderRepository.SaveAsync();
                return;
            }
        }

        //If the order was registered for Doppelspiel, but should not do it, e.g. because it has not enough battery charge
        if (droppingHostOrder.IsRegisteredForDoppelspielDrop && !shouldPickNext)
        {
            droppingHostOrder.UnregisterFromDoppelspiel(ActionType.Drop);
            await hostOrderRepository.UpdateAsync(droppingHostOrder);
            await hostOrderRepository.SaveAsync();

            await QueueUnmatchedDoppelspielPicks(context, [droppingHostOrder.Target], droppingHostOrderCorrelationId);
            await hostOrderRepository.SaveAsync();
        }
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
            if (hostOrder.Drop is { FinishedDate: null, IsRegisteredForDoppelspiel: true })
            {
                await QueueUnmatchedDoppelspielPicks(context, [hostOrder.Target], hostOrder.CorrelationId);
            }
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

        logger.LogInformation("Station group AKL changed. Queuing order {OrderId} / {CorrelationId} that was kept back because of AKL station limitation", hostOrder.Hu, hostOrder.CorrelationId);
        await QueueHostOrder(context, hostOrder);
    }

    /// <summary>
    /// Handles the <see cref="ResetDoppelspielPartner"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<ResetDoppelspielPartner> context)
    {
        var hostOrder = await hostOrderRepository.GetByIdAsync(context.Message.CorrelationId);

        if (hostOrder.Pick?.FinishedDate == null)
        {
            return;
        }
        if (hostOrder is { IsRegisteredForDoppelspielDrop: true, HasQueuedDoppelspielpartner: false })
        {
            hostOrder.UnregisterFromDoppelspiel(ActionType.Drop);
            await hostOrderRepository.UpdateAsync(hostOrder);
        }
    }

    /// <summary>
    /// Handles the <see cref="TriggerDoppelspielPartnerQueueing"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<TriggerDoppelspielPartnerQueueing> context)
    {
        var (orderId, agvCorrelationId, pickNode) = context.Message;
        var hostOrder = await hostOrderRepository.GetByIdAsync(orderId);
        if (hostOrder.HasQueuedDoppelspielpartner) // has already queued its Doppelspielpartner
        {
            logger.LogInformation("Order {OrderCorrelationId} has already queued DoppelspielPartner", hostOrder.CorrelationId);
            await context.RespondAsync(new DoppelspielPartnerQueued(null));
            return;
        }
        List<string>? sources = configuration.DoppelspielPickTargets.GetValueOrDefault(hostOrder.Target);
        if (sources == null) // is not at station supposed to do late Doppelspiel
        {
            logger.LogInformation("Order {OrderCorrelationId} target is not eligible for Doppelspiel", hostOrder.CorrelationId);
            await context.RespondAsync(new DoppelspielPartnerQueued(null));
            return;
        }

        var pickingHostOrder = await hostOrderRepository.GetNextPickForSources(sources);
        if (pickingHostOrder == null)
        {
            logger.LogInformation("Order {OrderCorrelationId} has no next Doppelspiel pick", hostOrder.CorrelationId);
            await context.RespondAsync(new DoppelspielPartnerQueued(null));
            return;
        }

        logger.LogInformation("Try queueing order {HostOrder} with pick node {Node}", pickingHostOrder.CorrelationId, pickingHostOrder.HasUnknownDropTarget(configuration) ? null : pickNode);
        if (await QueueHostOrder(context, pickingHostOrder, agvCorrelationId, pickingHostOrder.HasUnknownDropTarget(configuration) ? null : pickNode))
        {
            await doppelspielSemaphore.ExecuteLockedAsync(async () =>
            {
                pickingHostOrder.SetDoppelspielpartner(hostOrder.CorrelationId, ActionType.Pick);
                hostOrder.SetDoppelspielpartner(pickingHostOrder.CorrelationId, ActionType.Drop);

                await hostOrderRepository.UpdateAsync(pickingHostOrder);
                await hostOrderRepository.UpdateAsync(hostOrder);
                await hostOrderRepository.SaveAsync();
                await context.RespondAsync(new DoppelspielPartnerQueued(pickingHostOrder.CorrelationId));
                logger.LogInformation("Queued order {HostOrder} with pick node {Node} as Doppelspielpartner for Order {DropOrder}", pickingHostOrder.CorrelationId, pickingHostOrder.HasUnknownDropTarget(configuration) ? null : pickNode, hostOrder.CorrelationId);
            });
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

    /// <inheritdoc cref="HostOrderCommandHandler"/>>
    public async Task Consume(ConsumeContext<TryAssignDelayedOrderToAgv> context)
    {
        var droppingHostOrder = await hostOrderRepository.GetCurrentHostOrderForAgv(context.Message.CorrelationId);
        if (droppingHostOrder == null)
        {
            logger.LogInformation("{Message}: No host order found for agv {AgvCorrelationId}", nameof(TryAssignDelayedOrderToAgv), context.Message.CorrelationId);
            await context.RespondAsync(new AssignedDelayedOrder(null));
            return;
        }
        if (droppingHostOrder.Target != configuration.PrismatAplTarget)
        {
            // not dropping at apl
            logger.LogInformation("{Message}: Host order {OrderCorrelationId} for agv {AgvCorrelationId} not dropping at APL", nameof(TryAssignDelayedOrderToAgv), droppingHostOrder.CorrelationId, context.Message.CorrelationId);
            await context.RespondAsync(new AssignedDelayedOrder(null));
            return;
        }
        List<string>? sources = configuration.DoppelspielPickTargets.GetValueOrDefault(droppingHostOrder.Target);
        if (sources == null) // is not at station supposed to do Doppelspiel
        {
            logger.LogInformation("{Message}: Host order {OrderCorrelationId} for agv {AgvCorrelationId} not heading towards Doppelspiel station", nameof(TryAssignDelayedOrderToAgv), droppingHostOrder.CorrelationId, context.Message.CorrelationId);
            await context.RespondAsync(new AssignedDelayedOrder(null));
            return;
        }
        var pickingHostOrder = await hostOrderRepository.GetNextPickForSources(sources);
        if (pickingHostOrder == null)
        {
            logger.LogInformation("{Message}: No delayed pick for agv {AgvCorrelationId} running host order {OrderCorrelationId} ", nameof(TryAssignDelayedOrderToAgv), context.Message.CorrelationId, droppingHostOrder.CorrelationId);
            await context.RespondAsync(new AssignedDelayedOrder(null));
            return;
        }

        logger.LogInformation("Try queueing order {HostOrder} with pick node {Node}", pickingHostOrder.CorrelationId, context.Message.PickStation.Name);
        if (await QueueHostOrder(context, pickingHostOrder, context.Message.AssignableAgv, context.Message.PickStation))
        {
            await context.RespondAsync(new AssignedDelayedOrder(pickingHostOrder.CorrelationId));
        }
        logger.LogInformation("Queueing order {HostOrder} with pick node {Node} failed", pickingHostOrder.CorrelationId, context.Message.PickStation.Name);
        await context.RespondAsync(new AssignedDelayedOrder(null));
    }

    /// <inheritdoc cref="HostOrderCommandHandler"/>>
    public async Task Consume(ConsumeContext<ConsiderForDoppelspiel> context)
    {
        var hostOrder = await hostOrderRepository.GetByIdAsync(context.Message.CorrelationId);
        if (hostOrder.CanRunDoppelspiel)
        {
            return;
        }
        hostOrder.ConsiderForDoppelspiel();
        await hostOrderRepository.UpdateAsync(hostOrder);
        await hostOrderRepository.SaveAsync();
        logger.LogInformation("Considered order {OrderCorrelationId} for Doppelspiel", hostOrder.CorrelationId);
    }

    /// <inheritdoc cref="HostOrderCommandHandler"/>>
    public async Task Consume(ConsumeContext<QueueUnmatchedDoppelspielPicks> context)
    {
        ICollection<string> targets = configuration.GetPrismatTargetForStationName(context.Message.Station);
        if (targets.Count == 0)
        {
            logger.LogWarning("No targets configured for station {Station}, cannot queue unmatched Doppelspiel picks", context.Message.Station);
            return;
        }
        await QueueUnmatchedDoppelspielPicks(context, targets);
        await hostOrderRepository.SaveAsync();
    }
}