using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Constants;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Node;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using AgvStatus = Gebhardt.Acc.Common.Domain.Models.AgvStatus;
using Cargo = Gebhardt.Acc.TransportOrder.Domain.Models.Agv.Cargo;

namespace Gebhardt.Acc.TransportOrder.Core.Services.EventHandler;

/// <summary>
/// Consumer for AgvPositionChanged.
/// </summary>
[PublicAPI]
public class AgvPositionChangedEventHandler : IConsumer<AgvPositionChanged>, IConsumer<AgvLastNodeIdChanged>
{
    private readonly ILogger<AgvPositionChangedEventHandler> _logger;
    private readonly IAgvDb _agvDb;
    private readonly IActionGroupRepository _actionGroupRepository;
    private readonly ITriggerRepository _triggerRepository;
    private readonly IMapper _mapper;

    /// <summary>
    /// Constructor
    /// </summary>
    public AgvPositionChangedEventHandler(
        ILogger<AgvPositionChangedEventHandler> logger,
        IAgvDb agvDb,
        IActionGroupRepository actionGroupRepository,
        ITriggerRepository triggerRepository,
        IMapper mapper)
    {
        _logger = logger;
        _agvDb = agvDb;
        _actionGroupRepository = actionGroupRepository;
        _triggerRepository = triggerRepository;
        _mapper = mapper;
    }

    /// <summary>
    /// Publish AgvReachedTrigger if trigger is assigned to last node id.>.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task Consume(ConsumeContext<AgvLastNodeIdChanged> context)
    {
        (var agvId, string? oldLastNodeId, string? newLastNodeId) = context.Message;
        await context.Publish(new UpdateNodeOccupancy(agvId, oldLastNodeId, newLastNodeId));

        if (newLastNodeId != null)
        {
            List<Location> triggerLocations = _triggerRepository.GetNodeTriggerLocations(newLastNodeId).ToList();
            if (triggerLocations.Count > 0)
            {
                await PublishAgvReachedOrLeftTrigger(context, agvId, triggerLocations, false);
            }
        }
        if (oldLastNodeId != null)
        {
            List<Location> triggerLocations = _triggerRepository.GetNodeTriggerLocations(oldLastNodeId).ToList();
            if (triggerLocations.Count > 0)
            {
                await PublishAgvReachedOrLeftTrigger(context, agvId, triggerLocations, true);
            }
        }
    }

    /// <summary>
    /// Publish AgvReachedTrigger if trigger is assigned to a polygon.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task Consume(ConsumeContext<AgvPositionChanged> context)
    {
        var (agvId, newAgvPosition, _, _) = context.Message;

        ICollection<PolygonTriggerResult> triggerLocations = await _triggerRepository.GetPolygonTriggerLocations(agvId, newAgvPosition);
        List<Location> enteredTriggers = triggerLocations.Where(l => l.Action == PolygonTriggerAction.Entered).Select(l => l.Location).ToList();
        List<Location> leftTriggers = triggerLocations.Where(l => l.Action == PolygonTriggerAction.Left).Select(l => l.Location).ToList();
        if (enteredTriggers.Count > 0)
        {
            await PublishAgvReachedOrLeftTrigger(context, agvId, enteredTriggers, false);
        }
        if (leftTriggers.Count > 0)
        {
            await PublishAgvReachedOrLeftTrigger(context, agvId, leftTriggers, true);
        }
    }

    private async Task PublishAgvReachedOrLeftTrigger(IPublishEndpoint context, Guid agvId, ICollection<Location> triggerLocations, bool leftTrigger) 
    {
        Agv? agv;
        try
        {
            agv = await _agvDb.Agvs.GetAgvAsync(agvId);
        }
        catch (AgvNotFoundException)
        {
            _logger.LogDebug("{MethodName}: Agv not yet stored in database", nameof(PublishAgvReachedOrLeftTrigger));
            return;
        }

        var agvData = await _agvDb.Data.GetAgvDataAsync(agvId);
        var agvStatusCommon = _mapper.Map<AgvStatus>(agv.AgvStatus);

        var currentActionGroup = await _actionGroupRepository.GetCurrentAssignedActionGroupAsync(agvId);
        var commonActionGroup = _mapper.Map<ActionGroup>(currentActionGroup);

        IReadOnlyCollection<Cargo> cargoList = await _agvDb.Cargos.GetCargosAsync(agvId);
        ICollection<Common.Domain.Models.Cargo> commonCargoList = cargoList.Select(cargo => _mapper.Map<Common.Domain.Models.Cargo>(cargo)).ToList();

        IList<AgvError> errorList = await _agvDb.Errors.GetActiveErrorsAsync(agvId);

        foreach (var triggerLocation in triggerLocations)
        {
            if (leftTrigger)
            {
                await context.Publish(new AgvLeftTrigger(triggerLocation.TriggerId, triggerLocation.ConfigurationParameters.ToDictionary(k => k.Name, v => v), agvData!, agvStatusCommon!, commonActionGroup, commonCargoList, errorList));
                continue;
            }
            await context.Publish(new AgvReachedTrigger(triggerLocation.TriggerId, triggerLocation.ConfigurationParameters.ToDictionary(k => k.Name, v => v), agvData!, agvStatusCommon!, commonActionGroup, commonCargoList, errorList));

            var trigger = await _triggerRepository.GetById(triggerLocation.TriggerId);
            _logger.LogAgvInformation(LogCodes.AgvReachedTrigger, agvId, "AGV Reached Trigger",
                $"AGV {agv.SerialNumber} reached trigger {trigger.DisplayName}",
                new Dictionary<string, object?>
                {
                    { LogLabels.AgvSerialNumber, agv.SerialNumber },
                    { LogLabels.TriggerName, trigger.DisplayName }
                });
        }
    }
}