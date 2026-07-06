using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.AgvTypes;
using Gebhardt.Acc.TransportOrder.Messages.Commands.AgvType;
using Gebhardt.Acc.TransportOrder.Messages.Requests.AgvType;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Handles <see cref="AgvType"/> related requests.
/// </summary>
[UsedImplicitly]
public class AgvTypeRequestHandler(IBus bus, IAgvDb agvDb, ILogger<AgvTypeRequestHandler> logger) :
    IConsumer<RequestAgvTypeAcknowledgement>,
    IConsumer<GetAgvType>,
    IConsumer<GetAllAgvTypes>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<RequestAgvTypeAcknowledgement> context)
    {
        var id = context.Message.CorrelationId;

        if (!await agvDb.AgvTypes.ExistsAsync(id))
        {
            throw new AgvTypeNotFoundException(id);
        }

        await bus.Publish(new AcknowledgeAgvType(id), context.CancellationToken);
        logger.LogInformation("Acknowledgement of agv type {Id} requested", id);
        await context.RespondAsync(new Success());
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<GetAgvType> context)
    {
        var agvType = await agvDb.AgvTypes.GetAgvTypeAsync(context.Message.CorrelationId, context.CancellationToken);

        await context.RespondAsync(new GotAgvType(context.Message.CorrelationId, agvType));
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<GetAllAgvTypes> context)
    {
        ICollection<AgvType> agvTypes = await agvDb.AgvTypes.GetAllAgvTypesAsync(context.CancellationToken);

        await context.RespondAsync(new GotAgvTypes(context.Message.CorrelationId, agvTypes));
    }
}