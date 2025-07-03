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

    /// <inheritdoc cref="ActionAbortedConsumer"/>>
    public ActionAbortedConsumer(ILogger<ActionAbortedConsumer> logger, IAgvRepository agvRepository)
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

        var agvInformation = await _agvRepository.GetAgv(assignedAgv);
        if (string.IsNullOrEmpty(agvInformation.SerialNumber))
        {
            throw new AssignedAgvNotFoundException(assignedAgv, agvInformation.SerialNumber, action);
        }
        string orderId = action.CorrelationId.ToString();
        var orderType = action.ActionType.ToOrderType();

        _logger.LogInformation("Received {MessageType} for {Action} of order {Order} assigned to agv {AgvId}/{AgvCorrelationId}", nameof(ActionAborted), action.CorrelationId, orderId, agvInformation.SerialNumber, assignedAgv);

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