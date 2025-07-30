using Gebhardt.Acc.Common.Exceptions;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Extensions;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Enums;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Services;

/// <summary>
/// Provides handlers for action events triggered by other services.
/// </summary>
[UsedImplicitly]
public class ActionAbortedConsumer : IConsumer<ActionAborted>
{
    private readonly ILogger<ActionAbortedConsumer> _logger;
    private readonly IAgvRepository _agvRepository;
    private readonly IActionRepository _actionRepository;

    /// <inheritdoc cref="ActionAbortedConsumer"/>>
    public ActionAbortedConsumer(ILogger<ActionAbortedConsumer> logger, IAgvRepository agvRepository, IActionRepository actionRepository)
    {
        _logger = logger;
        _agvRepository = agvRepository;
        _actionRepository = actionRepository;
    }

    /// <summary>
    /// Handles the <see cref="ActionAborted"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<ActionAborted> context)
    {
        var (assignedAgv, action, _) = context.Message;

        var agvInformation = await _agvRepository.GetAgv(assignedAgv);
        if (string.IsNullOrEmpty(agvInformation.SerialNumber))
        {
            throw new AssignedAgvNotFoundException(assignedAgv, agvInformation.SerialNumber, action);
        }
        string orderId = action.CorrelationId.ToString();
        _logger.LogInformation("Received {MessageType} for {Action} of order {Order} assigned to agv {AgvId}/{AgvCorrelationId}", nameof(ActionAborted), action.CorrelationId, orderId, agvInformation.SerialNumber, assignedAgv);

        Guid? vehicleOrderId = await _actionRepository.GetVehicleOrderId(action.CorrelationId);
        if (vehicleOrderId != null)
        {
            _logger.LogInformation("Replacing fleet management service internal order id {InternalCorrelationId}/{InternalOrderId} by vehicle order id {VehicleCorrelationId}/{VehicleOrderId}", action.CorrelationId, orderId, vehicleOrderId, vehicleOrderId.Value.ToString());
            orderId = vehicleOrderId.Value.ToString();
        }
        var orderType = action.ActionType.ToOrderType();

        // We are using the ActionCorrelationId as "orderId"
        // And we are transferring the Action's Guid
        await context.PublishMqtt(new MqttOrder(orderId)
        {
            SerialNumber = agvInformation.SerialNumber,
            OrderType = orderType,
            OrderId = orderId,
            RecordType = OrderRecordType.OrderCancel
        }, context.CancellationToken);
    }
}