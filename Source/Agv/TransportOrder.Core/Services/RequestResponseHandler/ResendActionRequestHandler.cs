using AutoMapper;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Handles ResendAction related request.
/// </summary>
[UsedImplicitly]
public class ResendActionRequestHandler(IMapper mapper, IAgvDb agvDb, IActionGroupRepository actionGroupRepository, IBus bus) :
    IConsumer<ResendAction>
{
    /// <summary>
    /// Handles the <see cref="ResendAction"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<ResendAction> context)
    {
        var agvId = context.Message.CorrelationId;
        var agv = await agvDb.Agvs.GetAgvAsync(agvId);
        var actionGroup = await actionGroupRepository.GetCurrentAssignedActionGroupAsync(agvId);

        if (actionGroup == null || actionGroup.State == ActionGroupState.Created)
        {
            await context.RespondAsync(new ActionNotFound());
            return;
        }

        var action = actionGroup.FirstNotCompletedAction;

        if (action == null)
        {
            await context.RespondAsync(new ActionNotFound());
            return;
        }

        await bus.Publish(new Common.Messages.ActionGroups.Commands.ResendAction(agvId, agv.SerialNumber, mapper.Map<AgvAction>(action)));
        await context.RespondAsync(new Success());
    }
}