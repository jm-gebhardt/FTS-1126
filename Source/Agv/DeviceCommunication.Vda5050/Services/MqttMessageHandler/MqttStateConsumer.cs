using AutoMapper;
using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Mqtt;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Parsers;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Services.MqttMessageHandler;

/// <summary>
/// Provides a consumer for <see cref="MqttState"/>.
/// </summary>
[UsedImplicitly]
public class MqttStateConsumer : IConsumer<MqttState>
{
    private readonly ILogger<MqttStateConsumer> _logger;
    private readonly StateParser _stateParser;
    private readonly IAgvRepository _agvRepository;

    ///<inheritdoc cref="MqttStateConsumer"/> 
    public MqttStateConsumer(ILogger<MqttStateConsumer> logger, IMapper mapper, IAgvRepository agvRepository)
    {
        _logger = logger;
        _stateParser = new StateParser(logger, mapper);
        _agvRepository = agvRepository;
    }

    /// <inheritdoc/> 
    public async Task Consume(ConsumeContext<MqttState> context)
    {
        _stateParser.Parse(context.Message);
        _logger.LogDebug("Received {MessageType} from {AgvId}", nameof(MqttState), _stateParser.SerialNumber);

        await SendAgvStatusAsync(context);
        await SendAgvPositionAsync(context);
        await SendAgvErrorStatusAsync(context);
        await SendAgvLoadStatusAsync(context);
        await SendOrderStatusAsync(context);
    }

    private async Task SendAgvStatusAsync(ConsumeContext<MqttState> context)
    {
        var agvStatusReceived = new AgvStatusReceived(
            _stateParser.CorrelationId,
            _stateParser.SerialNumber,
            _stateParser.AgvStatus
        );
        await context.Publish(agvStatusReceived, context.CancellationToken);
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
        var agvErrorStatusReceived = new AgvErrorStatusReceived(_stateParser.CorrelationId, _stateParser.AgvErrors ?? [], _stateParser.AgvPosition);
        await context.Publish(agvErrorStatusReceived, context.CancellationToken);
    }

    private async Task SendAgvLoadStatusAsync(ConsumeContext<MqttState> context)
    {
        await context.Publish(new AgvLoadStatusReceived(_stateParser.CorrelationId, _stateParser.Cargos));
    }

    private async Task SendOrderStatusAsync(ConsumeContext<MqttState> context)
    {
        if (context.Message.OrderId == string.Empty)
        {
            return;
        }
        try
        {
            var agv = await _agvRepository.GetAgvAsync(_stateParser.CorrelationId);
            if (agv.CurrentOrder == null)
            {
                _logger.LogInformation("Not sending order state, agv {AgvCorrelationId} has no current order", _stateParser.CorrelationId);
                return;
            }
            await context.Publish(new OrderStateReceived(agv.CurrentOrder.Value, _stateParser.OrderState));
        }
        catch (AgvNotFoundException)
        {
            _logger.LogInformation("Not sending order state, agv {AgvCorrelationId} not registered yet", _stateParser.CorrelationId);
        }
    }
}