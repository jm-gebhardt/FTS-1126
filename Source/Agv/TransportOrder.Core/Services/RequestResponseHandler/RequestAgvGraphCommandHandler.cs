using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Commands;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
///
/// </summary>
[UsedImplicitly]
public class RequestAgvGraphCommandHandler : IConsumer<RequestAgvGraph>
{
    private readonly IAgvDb _agvDb;
    private readonly IBus _bus;

    /// <summary>
    /// <inheritdoc cref="RequestAgvGraphCommandHandler"/>
    /// </summary>
    public RequestAgvGraphCommandHandler(IAgvDb agvDb, IBus bus)
    {
        _agvDb = agvDb;
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
        var agv = await _agvDb.Agvs.GetAgvAsync(correlationId);
        await _bus.Publish(new Common.Messages.AgvManagement.Commands.RequestAgvGraph(agv.CorrelationId, agv.SerialNumber));
        await context.RespondAsync(new RequestAgvGraphResult(agv.CorrelationId));
    }
}