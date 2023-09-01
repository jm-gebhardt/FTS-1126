using JetBrains.Annotations;
using MassTransit;
using TransportOrder.Domain.Repositories;
using TransportOrder.Messages.Requests.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Defines a handler for the <see cref="GetAgvPositions"/>
/// Returns the positions for all agvs in the registry
/// </summary>
[UsedImplicitly]
public class GetAgvPositionsRequestHandler : IConsumer<GetAgvPositions>
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
    /// <inheritdoc cref="GetAgvPositionsRequestHandler"/>
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task Consume(ConsumeContext<GetAgvPositions> context)
    {
        List<AgvPositionStatus> agvPositions = _agvPositionRegistry.GetAllPositions().Select(p => new AgvPositionStatus(p.CorrelationId, p.AgvId, p.AgvPosition)).ToList();
        await context.RespondAsync(new GotAgvPositions(agvPositions));
    }
}