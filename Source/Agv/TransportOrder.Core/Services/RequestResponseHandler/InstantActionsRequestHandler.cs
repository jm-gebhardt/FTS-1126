using Gebhardt.Acc.Common.Domain.Constants;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Gebhardt.Acc.TransportOrder.Messages.Requests.InstantActions;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using StartInstantAction = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.StartInstantAction;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Provides handlers for instant action related requests.
/// </summary>
[UsedImplicitly]
public class InstantActionsRequestHandler(ILogger<InstantActionsRequestHandler> logger, IAgvInstantActionDefinitionRepository repository, IAgvDb agvDb, IBus bus, ILoggingService loggingService) : 
    IConsumer<GetAgvInstantActionDefinitions>,
    IConsumer<GetAgvsInstantActionsDefinitions>,
    IConsumer<StartInstantAction>
{
    /// <summary>
    /// Handles the <see cref="GetAgvInstantActionDefinitions"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<GetAgvInstantActionDefinitions> context)
    {
        var agvCorrelationId = context.Message.AgvCorrelationId;
        var instantActionDefinitions = await repository.GetInstantActionDefinitionsForAgv(agvCorrelationId);
        await context.RespondAsync(new GotAgvInstantActionDefinitions(instantActionDefinitions));
    }

    /// <summary>
    /// Handles the <see cref="GetAgvsInstantActionsDefinitions"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<GetAgvsInstantActionsDefinitions> context)
    {
        var agvCorrelationIds = context.Message.AgvCorrelationIds;
        var instantActionDefinitions = await repository.GetInstantActionDefinitionsByAgv(agvCorrelationIds);
        await context.RespondAsync(new GotAgvsInstantActionDefinitions(instantActionDefinitions));
    }

    /// <summary>
    /// Handles the <see cref="GetAgvsInstantActionsDefinitions"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<StartInstantAction> context)
    {
        (var agvCorrelationId, string actionType, var parameters, var blockingType, string user) = context.Message;

        var agv = await agvDb.Agvs.GetAgvAsync(agvCorrelationId);

        var instantActionDefinition = await repository.GetInstantActionDefinition(actionType.ToGuid());
        if (instantActionDefinition == null)
        {
            throw new AgvInstantActionDefinitionNotFoundException(actionType);
        }

        instantActionDefinition.ValidateStartValues(agvCorrelationId, blockingType, parameters);

        await bus.Publish(new Common.Messages.AgvManagement.Commands.StartInstantAction()
        {
            CorrelationId = Guid.NewGuid(),
            AgvSerialNumber = agv.SerialNumber,
            AgvInstantActionType = actionType,
            BlockingType = blockingType,
            Parameters = parameters
        });

        await loggingService.LogAgv(logger, LogLevel.Information, LogCodes.AgvOperatorInteraction, agvCorrelationId, null, "Operator interaction", "Operator interacted with agv", new Dictionary<string, object?> { { LogLabels.User, user }, { LogLabels.Operation, LogCodes.AgvStartInstantAction } });
        await context.RespondAsync(new Success());
    }
}