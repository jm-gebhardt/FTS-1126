using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.AgvTypes;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Commands.AgvType;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Commands;
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
    IConsumer<GetAllAgvTypes>,
    IConsumer<RequestAgvTypeChangeAcknowledgement>
{
    private static AgvTypeReadModel ToReadModel(AgvType agvType, IReadOnlySet<Guid> typeIdsWithGraph) =>
        new(agvType.Id, agvType.AgvTypeIdentifier, agvType.Manufacturer, agvType.SeriesName, agvType.Length, agvType.Width, agvType.Acknowledged, agvType.ReportedByAgv, typeIdsWithGraph.Contains(agvType.Id), agvType.RawData);

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
        var typeIdsWithGraph = await agvDb.Agvs.GetTypeIdsWithGraphAsync();

        await context.RespondAsync(new GotAgvType(context.Message.CorrelationId, ToReadModel(agvType, typeIdsWithGraph)));
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<GetAllAgvTypes> context)
    {
        var agvTypes = await agvDb.AgvTypes.GetAllAgvTypesAsync(context.CancellationToken);
        var typeIdsWithGraph = await agvDb.Agvs.GetTypeIdsWithGraphAsync();
        var readModels = agvTypes.Select(agvType => ToReadModel(agvType, typeIdsWithGraph)).ToList();

        await context.RespondAsync(new GotAgvTypes(context.Message.CorrelationId, readModels));
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<RequestAgvTypeChangeAcknowledgement> context)
    {
        var agv = await agvDb.Agvs.GetAgvAsync(context.Message.CorrelationId);
        await agvDb.AgvTypes.ExistsAsync(agv.AgvTypeId.GetValueOrDefault());

        await bus.Publish(new AcknowledgeAgvTypeChange(agv.CorrelationId), context.CancellationToken);
        await context.RespondAsync(new Success());
    }
}