using AutoMapper;
using Gebhardt.Acc.Common.Domain.Constants;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Models.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Commands.MaintenanceLog;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Core.Services.EventHandler;

/// <summary>
/// Handles log entry related events.
/// </summary>
[UsedImplicitly]
public class LogEntryEventHandler(IMapper mapper, IAgvDb agvDb, IStationRepository stationRepository, IActionGroupRepository actionGroupRepository) : IConsumer<LogOccurred>
{
    /// <inheritdoc />
    public async Task Consume(ConsumeContext<LogOccurred> context)
    {
        var entry = context.Message.Entry;

        foreach (var category in context.Message.Enrichment)
        {
            entry = category switch
            {
                Categories.Agv => await EnrichAgvs(entry),
                Categories.Station => await EnrichStations(entry),
                Categories.Order => await EnrichOrders(entry),
                _ => entry
            };
        }

        await context.Publish(new PersistLogEntry(mapper.Map<LogEntry>(entry)));
    }

    private async Task<LogEntryDto> EnrichAgvs(LogEntryDto entry)
    {
        Guid? agvId = entry.AgvCorrelationId;
        if (agvId == null)
        {
            return entry;
        }

        var agv = await agvDb.Agvs.GetAgvAsync(agvId.Value);
        entry.Variables.TryAdd(LogLabels.AgvSerialNumber, agv.SerialNumber);
        return entry;
    }

    private async Task<LogEntryDto> EnrichStations(LogEntryDto entry)
    {
        Guid? stationId = entry.StationId;
        if (stationId == null)
        {
            return entry;
        }

        var station = await stationRepository.GetStation(stationId.Value);

        entry.Variables.TryAdd(LogLabels.StationName, station.Name);

        return entry;
    }

    private async Task<LogEntryDto> EnrichOrders(LogEntryDto entry)
    {
        Guid? actionGroupId = entry.ActionGroupCorrelationId;
        AgvAction? action = null;

        if (entry.Variables.TryGetValue(LogLabels.ActionId, out string? actionId))
        {
            action = await actionGroupRepository.GetActionByIdAsync(actionId.ToGuid());
            actionGroupId ??= action?.ParentId;
        }

        if (actionGroupId != null)
        {
            entry.ActionGroupCorrelationId ??= actionGroupId;

            var actionGroup = await actionGroupRepository.GetActionGroupAsync(actionGroupId.Value);
            if (actionGroup != null)
            {
                entry.Variables.TryAdd(LogLabels.OrderId, actionGroup.OrderId);
            }
        }

        if (action != null)
        {
            entry.Variables.TryAdd(LogLabels.ActionType, action.ActionType.ToString());
        }
        if (entry.Variables.TryGetValue(LogLabels.OriginalActionId, out string? originalActionId))
        {
            var originalAction = await actionGroupRepository.GetActionByIdAsync(originalActionId.ToGuid());
            if (originalAction != null)
            {
                entry.Variables.TryAdd(LogLabels.OriginalActionType, originalAction.ActionType.ToString());
            }
        }
        
        return entry;
    }
}