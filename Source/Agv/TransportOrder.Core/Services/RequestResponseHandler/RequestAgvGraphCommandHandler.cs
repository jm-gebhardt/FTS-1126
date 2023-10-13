using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Commands;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
///
/// </summary>
[UsedImplicitly]
public class RequestAgvGraphCommandHandler : IConsumer<RequestAgvGraph>
{
    private readonly IAgvRepository _agvRepository;
    private readonly IBus _bus;

    /// <summary>
    /// <inheritdoc cref="RequestAgvGraphCommandHandler"/>
    /// </summary>
    public RequestAgvGraphCommandHandler(IAgvRepository agvRepository, IBus bus)
    {
        _agvRepository = agvRepository;
        _bus = bus;
    }

    /// <summary>
    /// Handles the <see cref="RequestAgvGraph"/> command.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="AgvNotFoundException"></exception>
    public async Task Consume(ConsumeContext<RequestAgvGraph> context)
    {
        var correlationId = context.Message.CorrelationId;
        var agv = await _agvRepository.GetAgvAsync(correlationId);
        await _bus.Publish(new Common.Messages.AgvManagement.Commands.RequestAgvGraph(agv.CorrelationId, agv.AgvId));
        await context.RespondAsync(new RequestAgvGraphResult(agv.CorrelationId));
    }
}