using AutoMapper;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Cargo = Gebhardt.Acc.TransportOrder.Domain.Models.Agv.Cargo;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.TransportOrder.Domain.Constants;

namespace Gebhardt.Acc.TransportOrder.Core.Services.EventHandler;

/// <summary>
/// Consumer for AgvLastNodeIdChanged.
/// </summary>
[PublicAPI]
public class AgvLastNodeIdChangedEventHandler : IConsumer<AgvLastNodeIdChanged>
{
    private readonly ILogger<AgvPositionReceivedEventHandler> _logger;
    private readonly IAgvDb _agvDb;
    private readonly IActionGroupRepository _actionGroupRepository;
    private readonly ITriggerRepository _triggerRepository;
    private readonly IMapper _mapper;

    /// <summary>
    /// Constructor
    /// </summary>
    public AgvLastNodeIdChangedEventHandler(
        ILogger<AgvPositionReceivedEventHandler> logger,
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
        if (context.Message.NewLastNodeId != null)
        {
            List<Guid> triggersOnNode = _triggerRepository.GetTriggersAssignedToNode(context.Message.NewLastNodeId).ToList();
            if (triggersOnNode.Count > 0)
            {
                var agvId = context.Message.CorrelationId;
                var agv = await _agvDb.Agvs.GetAgvAsync(agvId);
                var agvData = await _agvDb.Data.GetAgvDataAsync(agvId);
                var agvStatusCommon = _mapper.Map<AgvStatus>(agv.AgvStatus);

                var currentActionGroup = await _actionGroupRepository.GetCurrentAssignedActionGroupAsync(agvId);
                var commonActionGroup = _mapper.Map<ActionGroup>(currentActionGroup);

                IReadOnlyCollection<Cargo> cargoList = await _agvDb.Cargos.GetCargosAsync(agvId);
                ICollection<Common.Domain.Models.Cargo> commonCargoList = cargoList.Select(cargo => _mapper.Map<Common.Domain.Models.Cargo>(cargo)).ToList();

                IList<AgvError> errorList = await _agvDb.Errors.GetActiveErrorsAsync(agvId);

                foreach (var triggerId in triggersOnNode)
                {
                    await context.Publish(new AgvReachedTrigger(triggerId, agvData!, agvStatusCommon!, commonActionGroup, commonCargoList, errorList));
                    var trigger = await _triggerRepository.GetById(triggerId);
                    _logger.LogAgvInformation(LogCodes.AgvReachedTrigger, agvId, "AGV Reached Trigger", $"AGV {agv.SerialNumber} reached trigger {trigger.DisplayName}");
                }
            }
        }
    }
}
