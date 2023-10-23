using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services;

/// <summary>
/// Simulate finishing of host orders
/// </summary>
public class AgvStateConsumer : IConsumer<MqttState>
{
    private readonly ILogger<AgvStateConsumer> _logger;
    private readonly IAgvRegistry _agvRegistry;
    private readonly IMapper _mapper;

    /// <summary>
    /// Create consumer for agv status message (<see cref="MqttState"/>)
    /// </summary>
    /// <param name="logger">Mandatory logger facility.</param>
    /// <param name="agvRegistry"></param>
    /// <param name="mapper"></param>
    [PublicAPI]
    public AgvStateConsumer(ILogger<AgvStateConsumer> logger, IAgvRegistry agvRegistry, IMapper mapper)
    {
        _logger = logger;
        _agvRegistry = agvRegistry;
        _mapper = mapper;
    }

    /// <summary>
    /// Translate to AGV status message to AgvStatusReceived message
    /// </summary>
    public async Task Consume(ConsumeContext<MqttState> context)
    {
        var message = context.Message;
        string serialNumber = GetSerialNumber(context, message);
        var agvCorrelationId = serialNumber.ToGuid();

        await SendAgvStatusAsync(context, agvCorrelationId, message, serialNumber);
        await SendAgvPositionAsync(context, agvCorrelationId, serialNumber);
        await SendAgvErrorStatusAsync(context, agvCorrelationId, message);
    }

    private string GetSerialNumber(ConsumeContext<MqttState> context, MqttState message)
    {
        string serialNumber = GetSerialNumber(message);

        if (string.IsNullOrEmpty(serialNumber))
        {
            _logger.LogError("Missing serialNumber in {MessageType} message: {Status}", nameof(MqttState), context.Message);
            throw new InvalidOperationException($"Cannot handle {typeof(MqttState)} message, serialNumber must not be null or empty");
        }
        _logger.LogDebug("{MessageType} from {AgvId} received", nameof(MqttState), serialNumber);
        return serialNumber;
    }

    private async Task SendAgvStatusAsync(ConsumeContext<MqttState> context, Guid agvCorrelationId, MqttState message, string serialNumber)
    {
        var agvStatus = new AgvStatus(
            agvCorrelationId,
            GetOrderId(message),
            GetBusy(message),
            GetAutomatic(message),
            GetBatteryCharge(message),
            GetCharging(message),
            GetDisturbed(message),
            GetLastNodeId(message)
        );

        var agvStatusReceived = new AgvStatusReceived(
            agvCorrelationId,
            serialNumber,
            agvStatus
        );
        await context.Publish(agvStatusReceived, context.CancellationToken);

        var agvInformation = _agvRegistry.GetAgvInformation(agvStatusReceived.CorrelationId);
        var agvStatusReceivedFromRegistry = agvInformation?.AgvStatusReceived;

        if (agvStatusReceivedFromRegistry != null)
        {
            bool oldCharging = agvStatusReceivedFromRegistry.AgvStatus.Charging;
            bool newCharging = GetCharging(message);

            if (!oldCharging && newCharging)
            {
                await context.Publish(new AgvStartedCharging(agvStatusReceived.CorrelationId));
            }
            else if (oldCharging && !newCharging)
            {
                await context.Publish(new AgvStoppedCharging(agvStatusReceived.CorrelationId));
            }
        }

        FeedAgvRegistry(agvCorrelationId, agvStatusReceived);
    }

    private static async Task SendAgvPositionAsync(ConsumeContext<MqttState> context, Guid agvCorrelationId, string serialNumber)
    {
        if (context.Message.AgvPosition != null)
        {
            await context.Publish(new AgvPositionReceived(
                agvCorrelationId,
                serialNumber,
                new AgvPosition(
                    context.Message.AgvPosition.PositionInitialized,
                    context.Message.AgvPosition.X,
                    context.Message.AgvPosition.Y * -1, // Special treatment for Karis AGV. Y Axis is inverted
                    0f,
                    Mathematics.ConvertToRadians(90.0f) - context.Message.AgvPosition.Theta, // Special treatment for Karis AGV. 90� in our System (AGV facing right) is 0� in our world
                    context.Message.AgvPosition.DeviationRange
                )), context.CancellationToken);
        }
    }

    private async Task SendAgvErrorStatusAsync(ConsumeContext<MqttState> context, Guid agvCorrelationId, MqttState message)
    {
        var agvErrorStatusReceived = new AgvErrorStatusReceived(agvCorrelationId, message.Errors?.Select(e =>
        {
            var agvError = _mapper.Map<AgvError>(e);
            // overwrite AgvCorrelationId in a 2. step, as we are mapping from two sources to one destination, which isn't supported by AutoMapper
            _mapper.Map(agvCorrelationId, agvError);
            return agvError;
        }).ToList());

        await context.Publish(agvErrorStatusReceived, context.CancellationToken);
    }

    private void FeedAgvRegistry(Guid correlationId, AgvStatusReceived agvStatusReceived)
    {
        _agvRegistry.AddOrUpdateAgvStatus(correlationId, agvStatusReceived);
    }

    private string GetSerialNumber(MqttState message)
    {
        return message.SerialNumber;
    }

    private string GetOrderId(MqttState message)
    {
        return message.OrderId;
    }

    private bool GetBusy(MqttState message)
    {
        if (message.ActionStates == null)
        {
            return false;
        }

        foreach (var actionState in message.ActionStates)
        {
            switch (actionState.MqttActionStatus)
            {
                case MqttActionStatus.Failed: break;
                case MqttActionStatus.Finished: break;
                case MqttActionStatus.Initializing: return true;
                case MqttActionStatus.Running: return true;
                case MqttActionStatus.Waiting: break;
            }
        }

        return false;
    }

    private bool GetAutomatic(MqttState message)
    {
        switch (message.MqttOperatingMode)
        {
            case MqttOperatingMode.Automatic: return true;
            case MqttOperatingMode.Manual: return false;
            case MqttOperatingMode.Semiautomatic: return false;
            case MqttOperatingMode.Service: return false;
            case MqttOperatingMode.Teachin: return false;
            default: return false;
        }
    }

    private float GetBatteryCharge(MqttState message)
    {
        if (message.BatteryState == null)
        {
            return 0;
        }

        return message.BatteryState.BatteryCharge;
    }

    private bool GetCharging(MqttState message)
    {
        if (message.BatteryState == null)
        {
            return false;
        }

        return message.BatteryState.Charging;
    }

    private bool GetDisturbed(MqttState message)
    {
        if (message.SafetyState != null)
        {
            if (message.SafetyState.MqttEStop != MqttEStop.None)
            {
                return true;
            }

            if (message.SafetyState.FieldViolation)
            {
                return true;
            }
        }

        return false;
    }

    private string? GetLastNodeId(MqttState message)
    {
        if (!string.IsNullOrEmpty(message.LastNodeId))
        {
            return message.LastNodeId;
        }
        return null;
    }
}