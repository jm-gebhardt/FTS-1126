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
    private readonly IAgvRepository _agvRepository;
    private readonly IBus _bus;

    /// <summary>
    /// Initializes a new <see  cref="RequestAgvDataCommandHandler"/> instance.
    /// </summary>
    public RequestAgvDataCommandHandler(IAgvRepository agvRepository, IBus bus)
    {
        _agvRepository = agvRepository;
        _bus = bus;
    }

    /// <summary>
    /// Handles the <see cref="RequestAgvData"/> request.
    /// </summary>
    /// <exception cref="AgvNotFoundException"></exception>
    public async Task Consume(ConsumeContext<RequestAgvData> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;

        var agv = await _agvRepository.GetAgvAsync(agvCorrelationId);
        await _bus.Publish(new Common.Messages.AgvManagement.Commands.RequestAgvData(agv.CorrelationId, agv.AgvId));
        await context.RespondAsync(new GotAgvData(agv.CorrelationId));
    }
}