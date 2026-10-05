using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agvs.Queries;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Provides handlers for agv position requests.
/// </summary>
[UsedImplicitly]
public class GetAgvPositionsRequestHandler :
    IConsumer<GetAgvPositions>,
    IConsumer<GetAgvPosition>
{
    private readonly IAgvPositionRepository _agvPositionRepository;

    /// <summary>
    /// <inheritdoc cref="GetAgvPositionsRequestHandler"/>
    /// </summary>
    /// <param name="agvPositionRepository"></param>
    public GetAgvPositionsRequestHandler(IAgvPositionRepository agvPositionRepository)
    {
        _agvPositionRepository = agvPositionRepository;
    }

    /// <summary>
    /// Handles the <see cref="GetAgvPositions"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<GetAgvPositions> context)
    {
        var selectedAgvs = context.Message.CorrelationIds;

        var fetchedPositions = _agvPositionRepository.GetSelectedAgvsPositions(selectedAgvs);
        var agvPositions = fetchedPositions.Select(p => new Messages.MessageContent.AgvPositionStatus(p.CorrelationId, p.SerialNumber, p.AgvPosition)).ToList();
        await context.RespondAsync(new GotAgvPositions(agvPositions));
    }

    /// <summary>
    /// Handles the <see cref="GetAgvPosition"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<GetAgvPosition> context)
    {
        var correlationId = context.Message.CorrelationId;
        var agvPosition = _agvPositionRepository.GetPosition(correlationId);
        await context.RespondAsync(new GotAgvPosition(new Messages.MessageContent.AgvPositionStatus(agvPosition.CorrelationId, agvPosition.SerialNumber, agvPosition.AgvPosition)));
    }
}