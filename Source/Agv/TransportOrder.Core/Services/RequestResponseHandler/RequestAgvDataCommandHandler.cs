using JetBrains.Annotations;
using MassTransit;
using TransportOrder.Domain.Exceptions;
using TransportOrder.Domain.Repositories;
using TransportOrder.Messages.Requests.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Defines a handler for the EmitRequestDataRequest
/// Gets agv from agvRegistry for specific correlationId, emits RequestData telegram when receiving patch request.
/// </summary>
[UsedImplicitly]
public class RequestAgvDataCommandHandler : IConsumer<global::TransportOrder.Messages.Commands.Agv.RequestAgvData>
{
    private readonly IAgvRegistry _agvRegistry;
    private readonly IBus _bus;

    /// <summary>
    /// <inheritdoc cref="RequestAgvDataCommandHandler"/>
    /// </summary>
    /// <param name="agvRegistry"></param>
    /// <param name="bus"></param>
    public RequestAgvDataCommandHandler(IAgvRegistry agvRegistry, IBus bus)
    {
        _agvRegistry = agvRegistry;
        _bus = bus;
    }

    /// <summary>
    /// <inheritdoc cref="RequestAgvDataCommandHandler"/>
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    /// <exception cref="AgvNotFoundException"></exception>
    public async Task Consume(ConsumeContext<global::TransportOrder.Messages.Commands.Agv.RequestAgvData> context)
    {
        var request = context.Message;
        var agv = _agvRegistry.GetAgv(request.CorrelationId);

        if (agv == null)
        {
            throw new AgvNotFoundException(request.CorrelationId);
        }

        await _bus.Publish(new Common.Messages.AgvManagement.Commands.RequestAgvData(agv.CorrelationId, agv.AgvId));
        await context.RespondAsync(new GotAgvData(agv.CorrelationId));
    }
}