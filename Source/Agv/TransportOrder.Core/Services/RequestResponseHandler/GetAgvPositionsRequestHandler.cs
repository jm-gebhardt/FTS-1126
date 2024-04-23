using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
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
        ICollection<Guid> selectedAgvs = context.Message.CorrelationIds;

        ICollection<AgvPositionStatus> fetchedPositions = _agvPositionRepository.GetSelectedAgvsPositions(selectedAgvs);
        if (fetchedPositions.Count != selectedAgvs.Count)
        {
            throw new AgvsNotFoundException(selectedAgvs.Except(fetchedPositions.Select(p => p.CorrelationId)).ToList());
        }
        List<Messages.MessageContent.AgvPositionStatus> agvPositions = fetchedPositions.Select(p => new Messages.MessageContent.AgvPositionStatus(p.CorrelationId, p.SerialNumber, p.AgvPosition)).ToList();
        await context.RespondAsync(new GotAgvPositions(agvPositions));
    }

    /// <summary>
    /// Handles the <see cref="GetAgvPosition"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<GetAgvPosition> context)
    {
        var correlationId = context.Message.CorrelationId;
        var agvPosition = _agvPositionRepository.GetPosition(correlationId);
        if (agvPosition == null)
        {
            throw new AgvNotFoundException(correlationId);
        }
        await context.RespondAsync(new GotAgvPosition(new Messages.MessageContent.AgvPositionStatus(agvPosition.CorrelationId, agvPosition.SerialNumber, agvPosition.AgvPosition)));
    }
}