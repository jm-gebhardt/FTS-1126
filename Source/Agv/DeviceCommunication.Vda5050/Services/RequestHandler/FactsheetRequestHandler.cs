using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvActions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvTypes;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Factsheets;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Services.RequestHandler;

/// <summary>
/// Handles factsheet-related requests.
/// </summary>
[UsedImplicitly]
public class FactsheetRequestHandler(
    IAgvActionRepository agvActionRepository,
    IAgvTypeRepository agvTypeRepository,
    IMapper mapper,
    IBus bus,
    ILogger<FactsheetRequestHandler> logger) :
    IConsumer<AddOrUpdateFactsheet>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<AddOrUpdateFactsheet> context)
    {
        (var agvCorrelationId, var factsheet, string factsheetRawJson) = context.Message;

        var agvType = mapper.Map<AgvType>(factsheet) with { RawData = factsheetRawJson };
        var agvTypeInfo = mapper.Map<AgvTypeInfo>(agvType);

        if (await agvTypeRepository.ExistsAsync(agvType.AgvTypeIdentifier))
        {
            throw new AgvTypeAlreadyExistsException(agvType.AgvTypeIdentifier);
        }

        await agvTypeRepository.AddAsync(agvType);
        await bus.Publish(new AgvTypeCreated(agvType.Id, agvTypeInfo));

        try
        {
            var agvData = mapper.Map<AgvData>(factsheet);
            var actions = mapper.Map<ICollection<AgvAction>>(factsheet.ProtocolFeatures.AgvActions);
            foreach (var action in actions)
            {
                action.AgvSerialNumber = factsheet.SerialNumber;
            }
            if (actions.Count > 0)
            {
                await agvActionRepository.AddOrUpdateActionsAsync(actions, factsheet.SerialNumber);
                await PublishInstantActions(agvCorrelationId, actions);
            }
            await bus.Publish(new AgvDataReceived(agvCorrelationId, factsheet.SerialNumber, agvData));
            await bus.Publish(new AgvAddedOrUpdated(agvCorrelationId, factsheet.SerialNumber));
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
        var instantActions = actions.Where(a => a.ActionScopes.Contains(ActionScope.Instant)).ToList();
        var publishTasks = instantActions.Select(i => bus.Publish(new AgvInstantActionDefinitionReceived(agvCorrelationId, mapper.Map<AgvInstantAction>(i))));
        await Task.WhenAll(publishTasks);
    }
}