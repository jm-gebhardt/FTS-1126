using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events;
using Gebhardt.StoreWare.Agv.WCSCommunication.Domain.Models.HostOrder;
using Gebhardt.StoreWare.Agv.WCSCommunication.Domain.Repositories;
using Gebhardt.StoreWare.Agv.WCSCommunication.Domain.Services;
using Gebhardt.StoreWare.Agv.WCSCommunication.Messages.Commands;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.WCSCommunication.Services.CommandHandler;

/// <summary>
/// Provides handlers for host order related commands.
/// </summary>
[UsedImplicitly]
public class HostOrderCommandHandler: 
    IConsumer<CreateHostOrder>,
    IConsumer<RemoveInvalidHostOrder>,
    IConsumer<FinishHostOrder>,
    IConsumer<FinishHostOrderCancellation>,
    IConsumer<HandleHostOrderStart>,
    IConsumer<HandleCancellationFailure>,
    IConsumer<HandleHostOrderFailure>
{
    private readonly IHostOrderRepository _hostOrderRepository;
    private readonly IHostCommunicator _hostCommunicator;

    /// <summary>
    /// Initializes a new <see cref="HostOrderCommandHandler"/> instance.
    /// </summary>
    public HostOrderCommandHandler(IHostOrderRepository hostOrderRepository, IHostCommunicator hostCommunicator)
    {
        _hostOrderRepository = hostOrderRepository;
        _hostCommunicator = hostCommunicator;
    }

    /// <summary>
    /// Handles the <see cref="CreateHostOrder"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<CreateHostOrder> context)
    {
        var orderDetails = context.Message.OrderDetails;

        var hostOrder = new HostOrder(orderDetails.CorrelationId, orderDetails.OrderId);
        hostOrder.AddAction(orderDetails.Source, ActionType.Pick);
        hostOrder.AddAction(orderDetails.Destination, ActionType.Drop);

        await _hostOrderRepository.AddAsync(hostOrder);

        var actionGroup = new ActionGroup(
            hostOrder.CorrelationId,
            hostOrder.OrderId,
            hostOrder.Actions.Select(a => new AgvAction(a.CorrelationId, a.ActionType, a.Node, hostOrder.CorrelationId)
                {Sequence = a.SequenceNumber}).ToList(),
            orderDetails.Creator.ConvertToActionGroupCreator(),
            orderDetails.CreationDate);
        await context.Publish(new QueueActionGroup(actionGroup, false));
    }

    /// <summary>
    /// Handles the <see cref="RemoveInvalidHostOrder"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<RemoveInvalidHostOrder> context)
    {
        var correlationId = context.Message.CorrelationId;
        var hostOrder = await _hostOrderRepository.GetByIdAsync(correlationId);
        await _hostOrderRepository.RemoveAsync(hostOrder);
        await _hostCommunicator.TransmitHostOrderHandlingFailure(hostOrder.OrderId, HostOrderHandlingFailedErrorCode.InvalidOrderData.ToString());
    }

    /// <summary>
    /// Handles the <see cref="FinishHostOrder"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<FinishHostOrder> context)
    {
        var (correlationId, timestamp) = context.Message;
        var hostOrder = await _hostOrderRepository.GetByIdAsync(correlationId);
        hostOrder.Finish(timestamp);
        await _hostOrderRepository.UpdateAsync(hostOrder);
        await _hostCommunicator.TransmitHostOrderHandlingSucceeded(hostOrder.OrderId);
    }

    /// <summary>
    /// Handles the <see cref="FinishHostOrderCancellation"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<FinishHostOrderCancellation> context)
    {
        var (correlationId, timestamp) = context.Message;
        var hostOrder = await _hostOrderRepository.GetByIdAsync(correlationId);
        hostOrder.Cancel(timestamp);
        await _hostOrderRepository.UpdateAsync(hostOrder);
        await _hostCommunicator.TransmitHostOrderCancellationSucceeded(hostOrder.OrderId);
    }

    /// <summary>
    /// Handles the <see cref="HandleHostOrderStart"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<HandleHostOrderStart> context)
    {
        (var correlationId, string assignedAgv, var timestamp) = context.Message;
        var hostOrder = await _hostOrderRepository.GetByIdAsync(correlationId);
        hostOrder.Start(timestamp);
        await _hostOrderRepository.UpdateAsync(hostOrder);
        await _hostCommunicator.TransmitHostOrderStarted(hostOrder.OrderId, assignedAgv);
    }

    /// <summary>
    /// Handles the <see cref="HandleCancellationFailure"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<HandleCancellationFailure> context)
    {
        var (correlationId, error) = context.Message;
        var hostOrder = await _hostOrderRepository.GetByIdAsync(correlationId);
        await _hostCommunicator.TransmitHostOrderCancellationFailure(hostOrder.OrderId, error.ToString());
    }

    /// <summary>
    /// Handles the <see cref="HandleHostOrderFailure"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<HandleHostOrderFailure> context)
    {
        var (correlationId, error, timestamp) = context.Message;
        var hostOrder = await _hostOrderRepository.GetByIdAsync(correlationId);
        hostOrder.HandleFailure(timestamp);
        await _hostOrderRepository.UpdateAsync(hostOrder);
        await _hostCommunicator.TransmitHostOrderHandlingFailure(hostOrder.OrderId, error.ToString());
    }
}