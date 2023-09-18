using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Defines a handler for the EmitRequestDataRequest
/// Gets agv from agvRegistry for specific correlationId, emits RequestData telegram when receiving patch request.
/// </summary>
[UsedImplicitly]
public class RequestAgvDataCommandHandler : IConsumer<RequestAgvData>
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
    public async Task Consume(ConsumeContext<RequestAgvData> context)
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