using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvActions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvTypes;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.FactSheet;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Services.RequestHandler;

/// <summary>
/// Handles fact sheet-related requests.
/// </summary>
[UsedImplicitly]
public class FactSheetRequestHandler(
    IAgvActionRepository agvActionRepository,
    IAgvTypeRepository agvTypeRepository,
    IMapper mapper,
    IBus bus,
    ILogger<FactSheetRequestHandler> logger) :
    IConsumer<AddOrUpdateFactSheet>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<AddOrUpdateFactSheet> context)
    {
        (var agvCorrelationId, var factSheet, string factsheetRawJson) = context.Message;

        var agvType = mapper.Map<AgvType>(factSheet) with { RawData = factsheetRawJson };
        var agvTypeInfo = mapper.Map<AgvTypeInfo>(agvType);

        if (await agvTypeRepository.ExistsAsync(agvType.AgvTypeIdentifier))
        {
            throw new AgvTypeAlreadyExistsException(agvType.AgvTypeIdentifier);
        }

        await agvTypeRepository.AddAsync(agvType);
        await bus.Publish(new AgvTypeCreated(agvType.Id, agvTypeInfo));

        try
        {
            var agvData = mapper.Map<AgvData>(factSheet);
            var actions = mapper.Map<ICollection<AgvAction>>(factSheet.ProtocolFeatures.AgvActions);
            foreach (var action in actions)
            {
                action.AgvSerialNumber = factSheet.SerialNumber;
            }
            if (actions.Count > 0)
            {
                await agvActionRepository.AddOrUpdateActionsAsync(actions, factSheet.SerialNumber);
                await PublishInstantActions(agvCorrelationId, actions);
            }
            await bus.Publish(new AgvDataReceived(agvCorrelationId, factSheet.SerialNumber, agvData));
            await bus.Publish(new AgvAddedOrUpdated(agvCorrelationId, factSheet.SerialNumber));
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
            throw new InvalidFactsheetException(agvCorrelationId, ex.Message);
        }

        await context.RespondAsync(new Success());
    }

    private async Task PublishInstantActions(Guid agvCorrelationId, ICollection<AgvAction> actions)
    {
        List<AgvAction> instantActions = actions.Where(a => a.ActionScopes.Contains(ActionScope.Instant)).ToList();
        IEnumerable<Task> publishTasks = instantActions.Select(i => bus.Publish(new AgvInstantActionDefinitionReceived(agvCorrelationId, mapper.Map<AgvInstantAction>(i))));
        await Task.WhenAll(publishTasks);
    }
}