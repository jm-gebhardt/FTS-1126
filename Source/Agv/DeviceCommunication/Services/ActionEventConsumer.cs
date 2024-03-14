using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Enums;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Extensions;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services;

/// <summary>
/// Provides handlers for action events triggered by other services.
/// </summary>
[UsedImplicitly]
public class ActionEventConsumer : IConsumer<ActionAborted>
{
    private readonly ILogger<ActionEventConsumer> _logger;
    private readonly IAgvRepository _agvRepository;

    /// <inheritdoc cref="ActionEventConsumer"/>>
    public ActionEventConsumer(ILogger<ActionEventConsumer> logger, IAgvRepository agvRepository)
    {
        _logger = logger;
        _agvRepository = agvRepository;
    }

    /// <summary>
    /// Handles the <see cref="ActionAborted"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<ActionAborted> context)
    {
        var (assignedAgv, action) = context.Message;

        var agvInformation = _agvRepository.GetAgvInformation(assignedAgv);
        if (agvInformation == null || agvInformation.AgvStatusReceived!.SerialNumber.IsNullOrEmpty())
        {
            throw new AssignedAgvNotFoundException(assignedAgv, agvInformation!.AgvStatusReceived!.SerialNumber, action);
        }
        string orderId = action.CorrelationId.ToString();
        var orderType = action.ActionType.ToOrderType();

        _logger.LogInformation("Received {MessageType} for {Action} of order {Order} assigned to agv {AgvId}/{AgvCorrelationId}", nameof(ActionAborted), action.CorrelationId, orderId, agvInformation.AgvStatusReceived!.SerialNumber, assignedAgv);

        // We are using the ActionCorrelationId as "orderId"
        // And we are transferring the Action's Guid
        await context.PublishMqtt(new MqttOrder(orderId)
        {
            SerialNumber = agvInformation.AgvStatusReceived!.SerialNumber,
            OrderType = orderType,
            OrderId = orderId,
            RecordType = OrderRecordType.OrderCancel
        }, context.CancellationToken);
    }
}