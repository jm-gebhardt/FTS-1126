using AutoMapper;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.DeviceCommunication.MessageParsers;
using Gebhardt.Acc.DeviceCommunication.Messages.MqttMessages;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.Services;

/// <summary>
/// Provides a consumer for <see cref="MqttState"/>.
/// </summary>
[UsedImplicitly]
public class AgvStateConsumer : IConsumer<MqttState>
{
    private readonly ILogger<AgvStateConsumer> _logger;
    private readonly IAgvRepository _agvRepository;
    private readonly StateParser _stateParser;

    ///<inheritdoc cref="AgvStateConsumer"/> 
    public AgvStateConsumer(ILogger<AgvStateConsumer> logger, IAgvRepository agvRepository, IMapper mapper)
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
        _logger.LogInformation("Received {MessageType} from {AgvId}", nameof(MqttState), _stateParser.SerialNumber);

        await SendAgvStatusAsync(context);
        await SendAgvPositionAsync(context);
        await SendAgvErrorStatusAsync(context);
        await SendAgvLoadStatusAsync(context);
    }

    private async Task SendAgvLoadStatusAsync(ConsumeContext<MqttState> context)
    {
        await context.Publish(new AgvLoadStatusReceived(_stateParser.CorrelationId, _stateParser.Cargos));
    }

    private async Task SendAgvStatusAsync(ConsumeContext<MqttState> context)
    {
        var agvStatusReceived = new AgvStatusReceived(
            _stateParser.CorrelationId,
            _stateParser.SerialNumber,
            _stateParser.AgvStatus
        );
        await context.Publish(agvStatusReceived, context.CancellationToken);

        FeedAgvRepository(_stateParser.CorrelationId, agvStatusReceived);
    }

    private async Task SendAgvPositionAsync(ConsumeContext<MqttState> context)
    {
        if (context.Message.AgvPosition != null)
        {
            await context.Publish(new AgvPositionReceived(
                _stateParser.CorrelationId,
                _stateParser.SerialNumber,
                _stateParser.AgvPosition), context.CancellationToken);
        }
    }

    private async Task SendAgvErrorStatusAsync(ConsumeContext<MqttState> context)
    {
        var agvErrorStatusReceived = new AgvErrorStatusReceived(_stateParser.CorrelationId, _stateParser.AgvErrors, _stateParser.AgvPosition);
        await context.Publish(agvErrorStatusReceived, context.CancellationToken);
    }

    private void FeedAgvRepository(Guid correlationId, AgvStatusReceived agvStatusReceived)
    {
        _agvRepository.AddOrUpdateAgvStatus(correlationId, agvStatusReceived);
    }
}