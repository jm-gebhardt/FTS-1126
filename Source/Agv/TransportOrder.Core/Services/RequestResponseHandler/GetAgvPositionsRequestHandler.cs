using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agvs.Queries;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Provides handlers for agv position requests.
/// </summary>
[UsedImplicitly]
public class GetAgvPositionsRequestHandler :
    IConsumer<GetAgvPositions>,
    IConsumer<GetAgvPosition>
{
    private readonly IAgvPositionRegistry _agvPositionRegistry;

    /// <summary>
    /// <inheritdoc cref="GetAgvPositionsRequestHandler"/>
    /// </summary>
    /// <param name="agvPositionRegistry"></param>
    public GetAgvPositionsRequestHandler(IAgvPositionRegistry agvPositionRegistry)
    {
        _agvPositionRegistry = agvPositionRegistry;
    }

    /// <summary>
    /// Handles the <see cref="GetAgvPositions"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<GetAgvPositions> context)
    {
        ICollection<Guid> selectedAgvs = context.Message.CorrelationIds;

        ICollection<AgvPositionStatus> fetchedPositions = _agvPositionRegistry.GetSelectedAgvsPositions(selectedAgvs);
        if (fetchedPositions.Count != selectedAgvs.Count)
        {
            throw new AgvsNotFoundException(selectedAgvs.Except(fetchedPositions.Select(p => p.CorrelationId)).ToList());
        }
        List<Messages.MessageContent.AgvPositionStatus> agvPositions = fetchedPositions.Select(p => new Messages.MessageContent.AgvPositionStatus(p.CorrelationId, p.AgvId, p.AgvPosition)).ToList();
        await context.RespondAsync(new GotAgvPositions(agvPositions));
    }

    /// <summary>
    /// Handles the <see cref="GetAgvPosition"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<GetAgvPosition> context)
    {
        var correlationId = context.Message.CorrelationId;
        var agvPosition = _agvPositionRegistry.GetPosition(correlationId);
        if (agvPosition == null)
        {
            throw new AgvNotFoundException(correlationId);
        }
        await context.RespondAsync(new GotAgvPosition(new Messages.MessageContent.AgvPositionStatus(agvPosition.CorrelationId, agvPosition.AgvId, agvPosition.AgvPosition)));
    }
}