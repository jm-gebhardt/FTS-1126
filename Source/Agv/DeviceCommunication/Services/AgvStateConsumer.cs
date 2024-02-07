using System.Collections.ObjectModel;
using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Exceptions;
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
    private readonly IAgvRepository _agvRepository;
    private readonly IMapper _mapper;

    /// <summary>
    /// Create consumer for agv status message (<see cref="MqttState"/>)
    /// </summary>
    /// <param name="logger">Mandatory logger facility.</param>
    /// <param name="agvRepository"></param>
    /// <param name="mapper"></param>
    [PublicAPI]
    public AgvStateConsumer(ILogger<AgvStateConsumer> logger, IAgvRepository agvRepository, IMapper mapper)
    {
        _logger = logger;
        _agvRepository = agvRepository;
        _mapper = mapper;
    }

    /// <summary>
    /// Translate agv status message to AgvStatusReceived message.
    /// </summary>
    public async Task Consume(ConsumeContext<MqttState> context)
    {
        var message = context.Message;
        string serialNumber = GetSerialNumber(context, message);
        var agvCorrelationId = serialNumber.ToGuid();

        await SendAgvStatusAsync(context, agvCorrelationId, message, serialNumber);
        await SendAgvPositionAsync(context, agvCorrelationId, serialNumber);
        await SendAgvErrorStatusAsync(context, agvCorrelationId, message);
        await SendAgvLoadStatusAsync(context, agvCorrelationId, message);
    }

    private string GetSerialNumber(ConsumeContext<MqttState> context, MqttState message)
    {
        string serialNumber = GetSerialNumber(message);

        if (string.IsNullOrEmpty(serialNumber))
        {
            _logger.LogError("Missing serialNumber in {MessageType} message: {Status}", nameof(MqttState), context.Message);
            throw new SerialNumberNotSetException(nameof(MqttState));
        }
        _logger.LogDebug("{MessageType} from {AgvId} received", nameof(MqttState), serialNumber);
        return serialNumber;
    }

    private async Task SendAgvLoadStatusAsync(ConsumeContext<MqttState> context, Guid agvCorrelationId, MqttState message)
    {
        await context.Publish(new AgvLoadStatusReceived(agvCorrelationId, GetCargos(message)));
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
            GetBatteryVoltage(message),
            GetBatteryCurrent(message),
            GetBatteryTemperature(message),
            GetDisturbed(message),
            GetLastNodeId(message)
        );

        var agvStatusReceived = new AgvStatusReceived(
            agvCorrelationId,
            serialNumber,
            agvStatus
        );
        await context.Publish(agvStatusReceived, context.CancellationToken);

        FeedAgvRepository(agvCorrelationId, agvStatusReceived);
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
                    (float)Mathematics.ConvertToRadians(90.0f) - context.Message.AgvPosition.Theta, // Special treatment for Karis AGV. 90° in our System (AGV facing right) is 0° in our world
                    context.Message.AgvPosition.DeviationRange
                )), context.CancellationToken);
        }
    }

    private async Task SendAgvErrorStatusAsync(ConsumeContext<MqttState> context, Guid agvCorrelationId, MqttState message)
    {
        List<MqttError>? errors = message.Errors;

        if (errors?.Any(l => string.IsNullOrEmpty(l.ErrorType)) ?? false)
        {
            throw new ArgumentNullException(nameof(MqttError.ErrorType), "ErrorType must be set! This was the assumption at the time of implementation.");
        }

        var agvErrorStatusReceived = new AgvErrorStatusReceived(agvCorrelationId, errors?.Select(e =>
        {
            var agvError = _mapper.Map<AgvError>(e);
            // overwrite AgvCorrelationId in a 2. step, as we are mapping from two sources to one destination, which isn't supported by AutoMapper
            _mapper.Map(agvCorrelationId, agvError);
            return agvError;
        }).ToList());

        await context.Publish(agvErrorStatusReceived, context.CancellationToken);
    }

    private void FeedAgvRepository(Guid correlationId, AgvStatusReceived agvStatusReceived)
    {
        _agvRepository.AddOrUpdateAgvStatus(correlationId, agvStatusReceived);
    }

    private string GetSerialNumber(MqttState message) => message.SerialNumber;

    private string GetOrderId(MqttState message) => message.OrderId;

    private bool GetBusy(MqttState message)
    {
        if (message.ActionStates == null)
        {
            return false;
        }

        foreach (var actionState in message.ActionStates)
        {
            switch (actionState.ActionStatus)
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
        switch (message.OperatingMode)
        {
            case MqttOperatingMode.Automatic: return true;
            case MqttOperatingMode.Manual: return false;
            case MqttOperatingMode.Semiautomatic: return false;
            case MqttOperatingMode.Service: return false;
            case MqttOperatingMode.Teachin: return false;
            default: return false;
        }
    }

    private float GetBatteryCharge(MqttState message) => message.BatteryState?.BatteryCharge ?? -1f;

    private float GetBatteryVoltage(MqttState message) => message.BatteryState?.BatteryVoltage ?? -1f;

    private float GetBatteryCurrent(MqttState message) => message.BatteryState?.BatteryCurrent ?? -1f;

    private float GetBatteryTemperature(MqttState message) => message.BatteryState?.BatteryTemperature ?? -1f;

    private bool GetCharging(MqttState message) => message.BatteryState?.Charging ?? false;

    private bool GetDisturbed(MqttState message)
    {
        if (message.SafetyState != null)
        {
            if (message.SafetyState.EStop != MqttEStop.None)
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
        return !string.IsNullOrEmpty(message.LastNodeId) ? message.LastNodeId : null;
    }

    private Collection<Cargo> GetCargos(MqttState message)
    {
        List<MqttLoad>? loads = message.Loads;
        if (loads == null)
        {
            return new Collection<Cargo>();
        }

        if (loads.Any(l => string.IsNullOrEmpty(l.LoadId)))
        {
            throw new ArgumentNullException(nameof(MqttLoad.LoadId), "LoadId must be set! This was the assumption at the time of implementation.");
        }

        var cargos = _mapper.Map<Collection<Cargo>>(loads);
        return cargos;
    }
}