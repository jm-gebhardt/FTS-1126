using System.Numerics;
using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Trigger;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Core.Services.EventHandler;

/// <summary>
/// Provides handlers for trigger-related events
/// </summary>
[UsedImplicitly]
public class TriggerEventHandler(IAgvDb agvDb, ITriggerRepository triggerRepository, IGraphRepository graphRepository, IMapper mapper, ILogger<TriggerEventHandler> logger):
    IConsumer<TriggerConfigured>,
    IConsumer<ActionGroupStarted>
{
    private const double MaximumNodeTriggerDistance = 0.5;

    /// <summary>
    /// Handles the <see cref="TriggerConfigured"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<TriggerConfigured> context)
    {
        await context.Publish(new CreateTrigger(context.Message.Properties, context.Message.ConfigurationParameters));
    }

    /// <summary>
    /// Handles the <see cref="ActionGroupStarted"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<ActionGroupStarted> context)
    {
        var (actionGroup, actionGroupId, _) = context.Message;

        Guid? assignedAgv = actionGroup.AssignedAgv;

        if (assignedAgv == null)
        {
            logger.LogWarning("Cannot publish action group start trigger for {ActionGroup}: No agv assigned", actionGroupId);
            return;
        }

        var agvId = assignedAgv.Value;

        var agv = await agvDb.Agvs.TryGetAgvAsync(agvId);

        if (agv == null)
        {
            logger.LogWarning("Cannot publish action group start trigger for {AgvId},{ActionGroup}: No agv in database", agvId, actionGroupId);
            return;
        }

        var position = agvDb.Positions.GetPosition(agvId);

        if (position == null)
        {
            logger.LogWarning("Cannot publish action group start trigger for {Agv},{ActionGroup}: No agv position known", agvId, actionGroupId);
            return;
        }

        ICollection<Location> triggers = triggerRepository.GetPolygonTriggerLocations(position.AgvPosition);

        string? lastNodeId = agv.AgvStatus?.LastNodeId;

        if (lastNodeId != null)
        {
            var node = await graphRepository.TryGetNodeAsync(lastNodeId);

            bool isCloseToNode = node != null && (node.AsVector2() - new Vector2(position.AgvPosition.X, position.AgvPosition.Y)).Length() < MaximumNodeTriggerDistance;

            if (isCloseToNode)
            {
               triggers = triggers.Concat(triggerRepository.GetNodeTriggerLocations(lastNodeId)).ToList();
            }
        }

        if (triggers.Count == 0)
        {
            logger.LogInformation("No triggers for action group start {Agv},{ActionGroup}", agvId, actionGroupId);
            return;
        }

        var agvData = await agvDb.Data.GetAgvDataAsync(agvId);

        if (agvData == null)
        {
            logger.LogWarning("Cannot publish action group start trigger for {AgvId},{ActionGroup}: No agv data known", agvId, actionGroupId);
            return;
        }

        var agvStatusCommon = mapper.Map<AgvStatus>(agv.AgvStatus);

        IReadOnlyCollection<Domain.Models.Agv.Cargo> cargoList = await agvDb.Cargos.GetCargosAsync(agvId);
        ICollection<Cargo> commonCargoList = cargoList.Select(mapper.Map<Cargo>).ToList();

        IList<AgvError> errorList = await agvDb.Errors.GetActiveErrorsAsync(agvId);

        IEnumerable<Task> tasks = triggers.Select(trigger => context.Publish(new AgvStartedActionGroupOnTrigger(trigger.TriggerId, trigger.ConfigurationParameters.ToDictionary(k => k.Name, v => v), agvData, agvStatusCommon, actionGroup, commonCargoList, errorList)));

        await Task.WhenAll(tasks);
    }
}