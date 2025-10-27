using Gebhardt.Acc.Common.Domain.Constants;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.LogEntry;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Exceptions;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Enums;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Services;

/// <summary>
/// Consumer for MqttOrderEvent messages
/// </summary>
public class MqttOrderEventConsumer : IConsumer<MqttOrderEvent>
{
    private readonly ILogger<MqttOrderEventConsumer> _logger;
    private readonly IActionRepository _actionRepository;

    /// <inheritdoc cref="MqttOrderEventConsumer" />
    [PublicAPI]
    public MqttOrderEventConsumer(ILogger<MqttOrderEventConsumer> logger, IActionRepository actionRepository)
    {
        _logger = logger;
        _actionRepository = actionRepository;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<MqttOrderEvent> context)
    {
        var message = context.Message;
        var actionId = message.OrderId.ToGuid();

        _logger.LogInformation("Received {MessageType}.{OrderEventType} for {OrderId}", nameof(MqttOrderEvent), message.OrderEventType, actionId);

        // Only applicable if the internal order id and the one sent to the vehicle differ.
        Guid? internalOrderId = await _actionRepository.GetInternalOrderId(actionId);
        if (internalOrderId != null)
        {
            _logger.LogInformation("Replacing vehicle {OrderId} by fleet management internally used {InternalOrderId}", actionId, internalOrderId);
            actionId = internalOrderId.Value;
        }

        var agvCorrelationId = (message.SerialNumber ?? throw new SerialNumberNotSetException(nameof(MqttOrderEvent))).ToGuid();

        int logCode;
        int orderLogCode;

        switch (message.OrderEventType)
        {
            case OrderEventType.Started:
                await context.Publish(new ActionStarted(actionId, agvCorrelationId));
                logCode = LogCodes.AgvSentActionStarted;
                orderLogCode = LogCodes.AgvSentActionStartedForOrder;
                break;

            case OrderEventType.Completed:
                await context.Publish(new ActionCompleted(actionId, agvCorrelationId));
                await TryRemoveMapping(internalOrderId, message.OrderId.ToGuid());
                logCode = LogCodes.AgvSentActionCompleted;
                orderLogCode = LogCodes.AgvSentActionCompletedForOrder;
                break;

            case OrderEventType.Canceled:
                await context.Publish(new ActionCanceled(actionId, agvCorrelationId));
                await TryRemoveMapping(internalOrderId, message.OrderId.ToGuid());
                logCode = LogCodes.AgvSentActionCanceled;
                orderLogCode = LogCodes.AgvSentActionCanceledForOrder;
                break;

            case OrderEventType.CancelFailed:
                await context.Publish(new ActionCancelFailed(actionId, agvCorrelationId));
                logCode = LogCodes.AgvSentActionCancelFailed;
                orderLogCode = LogCodes.AgvSentActionCancelFailedForOrder;
                break;

            case OrderEventType.Failed:
                await context.Publish(new ActionFailed(actionId, agvCorrelationId));
                await TryRemoveMapping(internalOrderId, message.OrderId.ToGuid());
                logCode = LogCodes.AgvSentActionFailed;
                orderLogCode = LogCodes.AgvSentActionFailedForOrder;
                break;

            case OrderEventType.Updated:
                await context.Publish(new ActionUpdated(actionId, agvCorrelationId));
                logCode = LogCodes.AgvSentActionUpdated;
                orderLogCode = LogCodes.AgvSentActionUpdatedForOrder;
                break;

            default:
                throw new ArgumentOutOfRangeException($"Undefined OrderEventType {message.OrderEventType} for {message.OrderId}");
        }
        var logEntry = new LogEntryDto
        {
            ActionGroupCorrelationId = actionId, AgvCorrelationId = agvCorrelationId, Category = Categories.Agv, LogCode = logCode, LogLevel = nameof(LogLevel.Information), Timestamp = DateTime.UtcNow, Id = Guid.Empty,
            Variables = new Dictionary<string, string>
            {
                { LogLabels.OriginalActionId, message.OrderId.ToGuid().ToString() },
                { LogLabels.ActionId , actionId.ToString() }
            }
        };
        
        await context.Publish(new LogOccurred(logEntry, [Categories.Agv, Categories.Order]));

        logEntry.Category = Categories.Order;
        logEntry.LogCode = orderLogCode;

        await context.Publish(new LogOccurred(logEntry, [Categories.Agv, Categories.Order]));
    }

    private async Task TryRemoveMapping(Guid? internalOrderId, Guid vehicleOrderId)
    {
        if (internalOrderId != null)
        {
            await _actionRepository.RemoveVehicleAndInternalOrderIdMapping(vehicleOrderId);
        }
    }
}