using AutoMapper;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Mqtt;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Parsers;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Services.MqttMessageHandler;

/// <summary>
/// Provides handler for <see cref="MqttFactsheet"/>
/// </summary>
[UsedImplicitly]
public class MqttFactSheetConsumer:
    IConsumer<MqttFactsheet>
{
    private readonly FactsheetParser _parser;
    private readonly ILogger<MqttFactSheetConsumer> _logger;
    private readonly IAgvDataRepository _agvDataRepository;
    private readonly IAgvActionRepository _agvActionRepository;

    /// <inheritdoc cref="MqttFactSheetConsumer"/>
    public MqttFactSheetConsumer(ILogger<MqttFactSheetConsumer> logger, IAgvDataRepository agvDataRepository, IAgvActionRepository agvActionRepository, IMapper mapper)
    {
        _parser = new FactsheetParser(logger, mapper);
        _logger = logger;
        _agvDataRepository = agvDataRepository;
        _agvActionRepository = agvActionRepository;
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<MqttFactsheet> context)
    {
        _parser.Parse(context.Message);
        _logger.LogInformation("New fact sheet received for agv {AgvSerialNumber}/{AgvCorrelationId}", _parser.SerialNumber, _parser.SerialNumber);
        await _agvDataRepository.AddOrUpdateAsync(_parser.AgvData);
        if (_parser.Actions.Count > 0)
        {
            await _agvActionRepository.AddOrUpdateActionsAsync(_parser.Actions, _parser.SerialNumber);
        }
        await context.Publish(new AgvDataReceived(_parser.CorrelationId, _parser.SerialNumber, _parser.AgvData));
    }
}