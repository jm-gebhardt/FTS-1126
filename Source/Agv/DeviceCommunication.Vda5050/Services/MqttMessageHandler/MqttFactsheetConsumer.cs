using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvActions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Mqtt;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Parsers;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using AgvType = Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvTypes.AgvType;
using IAgvRepository = Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories.IAgvRepository;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Services.MqttMessageHandler;

/// <summary>
/// Provides handler for <see cref="MqttFactsheet"/>
/// </summary>
[UsedImplicitly]
public class MqttFactsheetConsumer :
    IConsumer<MqttFactsheet>
{
    private readonly FactsheetParser _parser;
    private readonly ILogger<MqttFactsheetConsumer> _logger;
    private readonly IAgvDataRepository _agvDataRepository;
    private readonly IAgvActionRepository _agvActionRepository;
    private readonly IAgvRepository _agvRepository;
    private readonly IAgvTypeRepository _agvTypeRepository;
    private readonly IMapper _mapper;

    /// <inheritdoc cref="MqttFactsheetConsumer"/>
    public MqttFactsheetConsumer(ILogger<MqttFactsheetConsumer> logger, IAgvDataRepository agvDataRepository, IAgvActionRepository agvActionRepository, IAgvRepository agvRepository, IAgvTypeRepository agvTypeRepository, IMapper mapper)
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
        var agv = await _agvRepository.GetAgvAsync(context.Message.SerialNumber.ToGuid());
        bool agvExists = agv != null;
        if (agvExists)
        {
            _parser.Agv = agv;
        }
        _parser.Parse(context.Message);
        string? routingKey = context.RoutingKey();
        if (routingKey != null)
        {
            _parser.Agv!.SetCommunicationSpecification(routingKey);
        }

        _logger.LogInformation("Factsheet received via mqtt for agv {AgvSerialNumber}/{AgvCorrelationId}", _parser.SerialNumber, _parser.SerialNumber);

        var incomingAgvType = _mapper.Map<AgvType>(context.Message);
        var existingAgvType = await _agvTypeRepository.GetAgvTypeAsync(incomingAgvType.AgvTypeIdentifier);
        bool incomingTypeExists = existingAgvType != null;
        if (!incomingTypeExists)
        {
            await AddAgvType(context, incomingAgvType);
        }
        bool incomingTypeIsCompatible = !incomingTypeExists || incomingAgvType.IsCompatibleWith(existingAgvType);
        if (!agvExists)
        {
            _logger.LogInformation("Agv {SerialNumber}/{AgvCorrelationId} did not exist prior to factsheet parsing, agv discovered with compatible type {IsTypeCompatible}", context.Message.SerialNumber, context.Message.SerialNumber.ToGuid(), incomingTypeIsCompatible);
            agv = _parser.Agv;
            await context.Publish(new AgvDiscovered(agv!.CorrelationId, agv, incomingAgvType.Id, incomingTypeIsCompatible));
            return;
        }
        if (agvExists && !(agv?.PublishedToFleetManager ?? false))
        {
            _logger.LogInformation("Agv {SerialNumber}/{AgvCorrelationId} did not exist prior to factsheet parsing, agv type is compatible {IsTypeCompatible}", context.Message.SerialNumber, context.Message.SerialNumber.ToGuid(), incomingTypeIsCompatible);
            agv = _parser.Agv;
            await context.Publish(new AgvTypeChanged(agv!.CorrelationId, incomingAgvType.Id, agv, incomingTypeIsCompatible));
            return;
        }

        bool agvTypeChanged = !string.Equals(agv!.VehicleTypeId, incomingAgvType.AgvTypeIdentifier, StringComparison.InvariantCultureIgnoreCase);
        if (!agvTypeChanged && !incomingTypeIsCompatible)
        {
            _logger.LogWarning("Received factsheet for agv {SerialNumber}/{AgvCorrelationId} that is incompatible with the existing agv type definition (no type change for agv)", agv.CorrelationId, agv.SerialNumber);
            await context.Publish(new InvalidAgvConfigurationReceived(agv.CorrelationId, agv.SerialNumber));
            return;
        }

        if (!agvTypeChanged && incomingTypeIsCompatible)
        {
            _logger.LogDebug("Received factsheet without change in type of agv {SerialNumber}/{AgvCorrelationId}", agv.CorrelationId, agv.SerialNumber);
            return;
        }
        var priorAgvType = await _agvTypeRepository.GetAgvTypeAsync(agv.VehicleTypeId);
        bool incomingAndPriorTypeCompatible = priorAgvType == null || incomingAgvType.IsCompatibleWith(priorAgvType);
        if (agvTypeChanged && !incomingTypeExists)
        {
            _logger.LogInformation("Received factsheet with agv type change, new type did not exist yet, needs type change confirmation");
            await context.Publish(new AgvTypeChanged(agv.CorrelationId, incomingAgvType.Id, agv, false, true));
            return;
        }
        if (agvTypeChanged && incomingTypeExists && !incomingAndPriorTypeCompatible)
        {
            _logger.LogWarning("Incompatible agv {SerialNumber}/{AgvCorrelationId} factsheet received via mqtt, including agv type change from {PreviousType}/{NewType}, types are compatible {IsTypeCompatible} ", agv.CorrelationId, agv.SerialNumber, agv.VehicleTypeId, incomingAgvType.AgvTypeIdentifier, incomingAndPriorTypeCompatible);
            await context.Publish(new AgvTypeChanged(agv.CorrelationId, incomingAgvType.Id, agv, false));
            return;
        }
        _logger.LogInformation("Received factsheet with agv type change, new type exists and is compatible with incoming type");
        await context.Publish(new AgvTypeChanged(agv.CorrelationId, incomingAgvType.Id, agv));
    }

    private async Task AddAgvType(ConsumeContext<MqttFactsheet> context, AgvType incomingAgvType)
    {
        await _agvDataRepository.AddOrUpdateAsync(_parser.AgvData);
        if (_parser.Actions.Count > 0)
        {
            await _agvActionRepository.AddOrUpdateActionsAsync(_parser.Actions, _parser.SerialNumber);
            await PublishInstantActions(context);
        }
        await context.Publish(new AgvDataReceived(_parser.CorrelationId, _parser.SerialNumber, _parser.AgvData));

        await _agvTypeRepository.AddAsync(incomingAgvType);
        await context.Publish(new AgvTypeCreated(incomingAgvType.Id, _mapper.Map<AgvTypeInfo>(incomingAgvType)));
    }

    private async Task PublishInstantActions(ConsumeContext<MqttFactsheet> context)
    {
        var instantActions = _parser.Actions.Where(a => a.ActionScopes.Contains(ActionScope.Instant)).ToList();
        var publishTasks = instantActions.Select(i => context.Publish(new AgvInstantActionDefinitionReceived(_parser.CorrelationId, _mapper.Map<AgvInstantAction>(i))));
        await Task.WhenAll(publishTasks);
    }
}