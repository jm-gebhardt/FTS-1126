using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.TransportOrder.Domain.Models.InstantActionDefinition;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;

/// <summary>
/// Handles instant action related messages.
/// </summary>
[UsedImplicitly]
public class InstantActionsCommandHandler(IAgvInstantActionDefinitionRepository repository, ILogger<InstantActionsCommandHandler> logger):
    IConsumer<AgvInstantActionDefinitionReceived>
{
    /// <summary>
    /// Handles the <see cref="AgvInstantActionDefinitionReceived"/> message.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvInstantActionDefinitionReceived> context)
    {
        var agv = context.Message.AgvCorrelationId;
        var correlationId = context.Message.CorrelationId;

        var instantActionDefinition = await repository.GetInstantActionDefinition(correlationId);
        if (instantActionDefinition == null)
        {
            instantActionDefinition = new InstantActionDefinition(correlationId, context.Message.InstantActionDefinition);
            instantActionDefinition.AddAgv(agv);
            if (await repository.Add(instantActionDefinition))
            {
                logger.LogInformation("Instant action definition {ActionType} received added for agv {AgvCorrelationId}", instantActionDefinition.ActionType, agv);
            }
            return;
        }

        instantActionDefinition.AddAgv(agv);
        if (await repository.Update(instantActionDefinition))
        {
            logger.LogInformation("Instant action definition {ActionType} updated for agv {AgvCorrelationId}", instantActionDefinition.ActionType, agv);
        }
    }
}