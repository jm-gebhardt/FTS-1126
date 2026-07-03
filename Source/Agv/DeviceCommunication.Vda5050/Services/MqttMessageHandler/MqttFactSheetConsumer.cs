using AutoMapper;
using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvActions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvTypes;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Mqtt;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Parsers;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using IAgvRepository = Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories.IAgvRepository;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Services.MqttMessageHandler;

/// <summary>
/// Provides handler for <see cref="MqttFactsheet"/>
/// </summary>
[UsedImplicitly]
public class MqttFactSheetConsumer :
    IConsumer<MqttFactsheet>
{
    private readonly FactsheetParser _parser;
    private readonly ILogger<MqttFactSheetConsumer> _logger;
    private readonly IAgvDataRepository _agvDataRepository;
    private readonly IAgvActionRepository _agvActionRepository;
    private readonly IAgvRepository _agvRepository;
    private readonly IAgvTypeRepository _agvTypeRepository;
    private readonly IMapper _mapper;

    /// <inheritdoc cref="MqttFactSheetConsumer"/>
    public MqttFactSheetConsumer(ILogger<MqttFactSheetConsumer> logger, IAgvDataRepository agvDataRepository, IAgvActionRepository agvActionRepository, IAgvRepository agvRepository, IAgvTypeRepository agvTypeRepository, IMapper mapper)
    {
        _parser = new FactsheetParser(logger, mapper);
        _logger = logger;
        _agvDataRepository = agvDataRepository;
        _agvActionRepository = agvActionRepository;
        _agvRepository = agvRepository;
        _agvTypeRepository = agvTypeRepository;
        _mapper = mapper;
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<MqttFactsheet> context)
    {
        try
        {
            var agv = await _agvRepository.GetRequiredAgvAsync(context.Message.SerialNumber.ToGuid());
            _parser.Agv = agv;
        }
        catch (AgvNotFoundException)
        {
            _logger.LogInformation("Agv {SerialNumber}/{AgvCorrelationId} did not exist prior to fact sheet parsing", context.Message.SerialNumber, context.Message.SerialNumber.ToGuid());
        }
        _parser.Parse(context.Message);
        string? routingKey = context.RoutingKey();
        if (routingKey != null)
        {
            _parser.Agv!.SetCommunicationSpecification(routingKey);
        }
        _logger.LogInformation("New fact sheet received for agv {AgvSerialNumber}/{AgvCorrelationId}", _parser.SerialNumber, _parser.SerialNumber);
        await _agvDataRepository.AddOrUpdateAsync(_parser.AgvData);
        await _agvRepository.AddOrUpdateAsync(_parser.Agv!);
        if (_parser.Actions.Count > 0)
        {
            await _agvActionRepository.AddOrUpdateActionsAsync(_parser.Actions, _parser.SerialNumber);
            await PublishInstantActions(context);
        }
        await context.Publish(new AgvDataReceived(_parser.CorrelationId, _parser.SerialNumber, _parser.AgvData));
        await context.Publish(new AgvAddedOrUpdated(_parser.CorrelationId, _parser.SerialNumber));

        var agvType = _mapper.Map<AgvType>(context.Message);
        if (!await _agvTypeRepository.ExistsAsync(agvType.AgvTypeIdentifier))
        {
            await _agvTypeRepository.AddAsync(agvType);
            await context.Publish(new AgvTypeCreated(agvType.Id, _mapper.Map<AgvTypeInfo>(agvType)));
        }
    }

    private async Task PublishInstantActions(ConsumeContext<MqttFactsheet> context)
    {
        List<AgvAction> instantActions = _parser.Actions.Where(a => a.ActionScopes.Contains(ActionScope.Instant)).ToList();
        IEnumerable<Task> publishTasks = instantActions.Select(i => context.Publish(new AgvInstantActionDefinitionReceived(_parser.CorrelationId, _mapper.Map<AgvInstantAction>(i))));
        await Task.WhenAll(publishTasks);
    }
}