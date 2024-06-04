using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
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
public class HostOrderCommandHandler(ILogger<HostOrderCommandHandler> logger, IHostOrderRepository hostOrderRepository, IHostCommunicator hostCommunicator) :
    IConsumer<CreateHostOrder>,
    IConsumer<FinishHostOrder>

{
    /// <summary>
    /// Handles the <see cref="CreateHostOrder"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<CreateHostOrder> context)
    {
        var orderDetails = context.Message.OrderDetails;
        string hu = context.Message.Hu;
       
        var hostOrder = new HostOrder(orderDetails.CorrelationId, orderDetails.OrderId, hu);
        hostOrder.AddAction(orderDetails.Source, ActionType.Pick);
        hostOrder.AddAction(orderDetails.Destination, ActionType.Drop);

        hostOrderRepository.Add(hostOrder);
        await hostOrderRepository.SaveAsync();
        logger.LogInformation("Host order for order {OrderId} created", orderDetails.OrderId);

        var actionGroup = new ActionGroup(
            hostOrder.CorrelationId,
            hostOrder.OrderId,
            hostOrder.Actions.Select(a => new AgvAction(a.CorrelationId, a.ActionType, a.Node, hostOrder.CorrelationId)
                { Sequence = a.SequenceNumber }).ToList(),
            orderDetails.Creator.ConvertToActionGroupCreator(),
            orderDetails.CreationDate);
        await context.Publish(new QueueActionGroup(actionGroup, false));
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
}