using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.HostOrder.Events;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
using Gebhardt.Acc.PrismatCommunication.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Domain.Services;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands.HostOrder;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Globalization;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.CustomerConfiguration;
using CancelHostOrder = Gebhardt.Acc.PrismatCommunication.Messages.Commands.HostOrder.CancelHostOrder;

namespace Gebhardt.Acc.PrismatCommunication.Services.CommandHandler;

/// <summary>
/// Provides handlers for host order related commands.
/// </summary>
[UsedImplicitly]
public class HostOrderCommandHandler(
    ILogger<HostOrderCommandHandler> logger,
    IHostOrderRepository hostOrderRepository,
    IHostCommunicator hostCommunicator,
    CustomerConfiguration configuration
) :
    IConsumer<CreateHostOrder>,
    IConsumer<CancelHostOrder>,
    IConsumer<CancelInvalidHostOrder>,
    IConsumer<HandleHostOrderStart>,
    IConsumer<FinishHostOrder>,
    IConsumer<FinishPick>,
    IConsumer<AbortHostOrder>,
    IConsumer<InvalidateHosterOrder>

{
    /// <summary>
    /// Handles the <see cref="CreateHostOrder"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<CreateHostOrder> context)
    {
        var orderDetails = context.Message.OrderDetails;
        string hu = context.Message.Hu;

        var hostOrder = new HostOrder(orderDetails.CorrelationId, orderDetails.OrderId, hu, orderDetails.CreationDate);

        var (pickInfoToAgv, dropInfoToAgv) = GeneratePickAndDropInformation(context.Message);
        logger.LogDebug("pickInfoToAgv: {PickInfoToAgv}", pickInfoToAgv == null ? "empty" : string.Join(", ", pickInfoToAgv.Select(kvp => $"{kvp.Key}: {kvp.Value}")));
        logger.LogDebug("dropInfoToAgv: {DropInfoToAgv}", dropInfoToAgv == null ? "empty" : string.Join(", ", dropInfoToAgv.Select(kvp => $"{kvp.Key}: {kvp.Value}")));

        hostOrder.AddAction(orderDetails.Source, ActionType.Pick, pickInfoToAgv);
        hostOrder.AddAction(orderDetails.Destination, ActionType.Drop, dropInfoToAgv);

        hostOrderRepository.Add(hostOrder);
        await hostOrderRepository.SaveAsync();
        if (hostOrder.CanceledDate != null)
        {
            return;
        }
        await CancelUnfinishedOrdersOfHu(context, hu, orderDetails.CreationDate);

        await context.Publish(new HostOrderCreated(hu, orderDetails));
    }

    private async Task CancelUnfinishedOrdersOfHu(IPublishEndpoint context, string hu, DateTime creationDate)
    {
        var hostOrders = await hostOrderRepository.GetLatestUnfinishedOrdersByHu(hu, creationDate);
        if (hostOrders.Count == 0)
        {
            logger.LogDebug("No unfinished host orders for HU {Hu}", hu);
            return;
        }
        foreach (var hostOrder in hostOrders)
        {
            hostOrder.Cancel(DateTime.UtcNow);

            logger.LogInformation("Trigger cancellation or abortion of host order {ActionGroupCorrelationId} for HU {Hu}", hostOrder.CorrelationId, hostOrder.Hu);
            await context.Publish(new CancelOrAbortActionGroup(hostOrder.CorrelationId));
        }
        await hostOrderRepository.SaveAsync();
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
            CustomerConfiguration.ErrorCodeNoRoute,
            configuration.OrderValidationErrors[CustomerConfiguration.ErrorCodeNoRoute],
            DateTime.UtcNow);
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
        logger.LogInformation("Finished host order {HostOrder} ", hostOrder.CorrelationId);
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
    /// Handles the <see cref="AbortHostOrder"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<AbortHostOrder> context)
    {
        var correlationId = context.Message.CorrelationId;
        logger.LogInformation("Aborting host order {ActionGroupCorrelationId}", correlationId);
        try
        {
            var hostOrder = await hostOrderRepository.GetByIdAsync(correlationId);
            hostOrder.Cancel(DateTime.UtcNow);
            await hostOrderRepository.UpdateAsync(hostOrder);
            await hostOrderRepository.SaveAsync();
        }
        catch (InvalidOperationException)
        {
            // not order known to prismat
        }
    }

    /// <inheritdoc/>>
    public async Task Consume(ConsumeContext<InvalidateHosterOrder> context)
    {
        var (correlationId, validationError) = context.Message;
        logger.LogInformation("Invalidating host order {ActionGroupCorrelationId} because of validation error {ValidationError}", correlationId, validationError);
        try
        {
            var hostOrder = await hostOrderRepository.GetByIdAsync(correlationId);
            var error = configuration.GetError(validationError);
            hostCommunicator.ReportOrderError(hostOrder.Hu, hostOrder.Source, hostOrder.Target, error.Code, error.Description, DateTime.UtcNow);
            hostOrder.Cancel(DateTime.UtcNow);
            await hostOrderRepository.UpdateAsync(hostOrder);
            await hostOrderRepository.SaveAsync();
        }
        catch (InvalidOperationException)
        {
            // not order known to prismat
        }
    }
}