using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.DeviceCommunication.Common.Configuration;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.MessageParsers;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Services;

/// <summary>
/// Handle data information from AGV.
/// </summary>
/// <remarks>
/// This is passed to the service AgvManagement to have kept DeviceCommunication as a simple proxy.
/// </remarks>
[UsedImplicitly]
public class AgvDataConsumer : IConsumer<MqttAgvData>
{
    private readonly ILogger<AgvDataConsumer> _logger;
    private readonly IConfigurationReader _reader;

    /// <summary>
    /// Create consumer with message handler delegate.
    /// </summary>
    public AgvDataConsumer(ILogger<AgvDataConsumer> logger, IConfigurationReader reader)
    {
        _logger = logger;
        _reader = reader;
    }

    /// <summary>
    /// Consume data message from AGV and transform this to an internal <see cref="AgvDataReceived"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Message content is invalid and cannot be processed.</exception>
    public async Task Consume(ConsumeContext<MqttAgvData> context)
    {
        var parser = new AgvDataParser(context.Message);
        _logger.LogInformation("Incoming AgvData: {AgvSerialNumber} / {CorrelationId}", context.Message.SerialNumber, context.Message.RequestId);

        try
        {
            await context.Publish(new AgvDataReceived(
                    parser.SerialNumber.ToGuid(),
                    parser.SerialNumber,
                    parser.AgvData)
                , context.CancellationToken);
            await GetInstantActionDefinitions(context, parser);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Message {MessageType} has invalid content: {AgvData} / {CorrelationId}", nameof(MqttAgvData), context.Message, context.CorrelationId);
            throw;
        }
    }

    private async Task GetInstantActionDefinitions(ConsumeContext<MqttAgvData> context, AgvDataParser parser)
    {
        var instantActionConfiguration = new InstantActionsConfiguration();
        instantActionConfiguration.Load(_reader);
        foreach (var instantActionDefinition in instantActionConfiguration.GetDefinitionsForAgv(parser.AgvData, parser.SerialNumber))
        {
            _logger.LogInformation("New instant action definition received: {AgvSerialNumber} / {ActionType}", parser.SerialNumber, instantActionDefinition.ActionType);
            await context.Publish(new AgvInstantActionDefinitionReceived(parser.SerialNumber.ToGuid(), instantActionDefinition));
        }
    }
}