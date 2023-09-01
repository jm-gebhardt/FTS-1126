using JetBrains.Annotations;
using MassTransit;
using TransportOrder.Domain.Exceptions;
using TransportOrder.Domain.Repositories;
using TransportOrder.Messages.Requests.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Defines a handler for the EmitRequestGraph
/// Gets the agv from agvRegistry and set status of agv from Discovered to Idle
/// </summary>
[UsedImplicitly]
public class RequestAgvGraphCommandHandler : IConsumer<global::TransportOrder.Messages.Commands.Agv.RequestAgvGraph>
{
    private readonly IAgvRegistry _agvRegistry;
    private readonly IBus _bus;

    /// <summary>
    /// <inheritdoc cref="RequestAgvGraphCommandHandler"/>
    /// </summary>
    /// <param name="agvRegistry"></param>
    /// <param name="bus"></param>
    public RequestAgvGraphCommandHandler(IAgvRegistry agvRegistry, IBus bus)
    {
        _agvRegistry = agvRegistry;
        _bus = bus;
    }

    /// <summary>
    /// <inheritdoc cref="RequestAgvGraphCommandHandler"/>
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    /// <exception cref="AgvNotFoundException"></exception>
    public async Task Consume(ConsumeContext<global::TransportOrder.Messages.Commands.Agv.RequestAgvGraph> context)
    {
        var request = context.Message;
        var agv = _agvRegistry.GetAgv(request.CorrelationId);

        if (agv == null)
        {
            throw new AgvNotFoundException(request.CorrelationId);
        }

        await _bus.Publish(new Common.Messages.AgvManagement.Commands.RequestAgvGraph(agv.CorrelationId, agv.AgvId));
        await context.RespondAsync(new RequestAgvGraphResult(agv.CorrelationId));
    }
}