using AutoMapper;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.MessageParsers;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Services;

/// <summary>
/// Provides a consumer for <see cref="MqttState"/>.
/// </summary>
[UsedImplicitly]
public class MqttStateConsumer : IConsumer<MqttState>
{
    private readonly ILogger<MqttStateConsumer> _logger;
    private readonly IAgvRepository _agvRepository;
    private readonly StateParser _stateParser;

    ///<inheritdoc cref="MqttStateConsumer"/> 
    public MqttStateConsumer(ILogger<MqttStateConsumer> logger, IAgvRepository agvRepository, IMapper mapper)
    {
        _logger = logger;
        _agvRepository = agvRepository;
        _stateParser = new StateParser(logger, mapper);
    }

    /// <summary>
    /// Translate agv status message to AgvStatusReceived message.
    /// </summary>
    public async Task Consume(ConsumeContext<MqttState> context)
    {
        _stateParser.Parse(context.Message);
        _logger.LogDebug("Received {MessageType} from {AgvId}", nameof(MqttState), _stateParser.SerialNumber);

        await SendAgvStatusAsync(context);
        await SendAgvPositionAsync(context);
        await SendAgvErrorStatusAsync(context);
        await SendAgvLoadStatusAsync(context);
        await SendWifiMetricsAsync(context);
    }

    private async Task SendAgvStatusAsync(ConsumeContext<MqttState> context)
    {
        var agvStatusReceived = new AgvStatusReceived(
            _stateParser.CorrelationId,
            _stateParser.SerialNumber,
            _stateParser.AgvStatus
        );
        await context.Publish(agvStatusReceived, context.CancellationToken);

        await FeedAgvRepository(_stateParser.CorrelationId, agvStatusReceived);
    }

    private async Task SendAgvPositionAsync(ConsumeContext<MqttState> context)
    {
        if (context.Message.AgvPosition != null)
        {
            await context.Publish(new AgvPositionReceived(
                _stateParser.CorrelationId,
                _stateParser.SerialNumber,
                _stateParser.AgvPosition)
                {
                    MessageCreatedDate = context.Message.Timestamp.UtcDateTime
                }
                , context.CancellationToken);
        }
    }

    private async Task SendAgvErrorStatusAsync(ConsumeContext<MqttState> context)
    {
        var agvErrorStatusReceived = new AgvErrorStatusReceived(_stateParser.CorrelationId, _stateParser.AgvErrors, _stateParser.AgvPosition);
        if (agvErrorStatusReceived.Errors?.Any(e => e.IsPushedManually())?? false)
        {
            _logger.LogInformation("Agv {AgvCorrelationId} moved in push or joystick mode", agvErrorStatusReceived.CorrelationId);
            await context.Publish(new AgvSetToPushOrJoystickMode(agvErrorStatusReceived.CorrelationId));
        }
        await context.Publish(agvErrorStatusReceived, context.CancellationToken);
    }

    private async Task SendAgvLoadStatusAsync(ConsumeContext<MqttState> context)
    {
        await context.Publish(new AgvLoadStatusReceived(_stateParser.CorrelationId, _stateParser.Cargos));
    }

    private async Task SendWifiMetricsAsync(ConsumeContext<MqttState> context)
    {
        if (_stateParser.WifiMetrics != null)
        {
            await context.Publish(new AgvWifiMetricsReceived(
                _stateParser.CorrelationId,
                _stateParser.SerialNumber,
                _stateParser.AgvPosition,
                _stateParser.WifiMetrics)
                {
                    MessageCreatedDate = context.Message.Timestamp.UtcDateTime
                }
                , context.CancellationToken);
            
            _logger.LogDebug("Published {MessageType} for {AgvId}", nameof(AgvWifiMetricsReceived), _stateParser.SerialNumber);
        }
    }

    private async Task FeedAgvRepository(Guid correlationId, AgvStatusReceived agvStatusReceived)
    {
        await _agvRepository.AddOrUpdate(correlationId, agvStatusReceived.SerialNumber);
    }
}