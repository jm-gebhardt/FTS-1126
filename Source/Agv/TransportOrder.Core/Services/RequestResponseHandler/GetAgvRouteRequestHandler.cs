using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Routing;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Defines a handler for GetAgvRoute request
/// </summary>
[UsedImplicitly]
public class GetAgvRouteRequestHandler : IConsumer<GetAgvRoute>, IConsumer<GetAgvRoutes>
{
    private readonly IAgvRouteRetrievalService _agvRouteRetrievalService;

    /// <inheritdoc cref="GetAgvRouteRequestHandler"/>
    public GetAgvRouteRequestHandler(IAgvRouteRetrievalService agvRouteRetrievalService)
    {
        _agvRouteRetrievalService = agvRouteRetrievalService;
    }

    /// <inheritdoc cref="GetAgvRouteRequestHandler"/>
    public async Task Consume(ConsumeContext<GetAgvRoute> context)
    {
        var agvRoute = await _agvRouteRetrievalService.GetAgvRouteAsync(context.Message.CorrelationId);
        await context.RespondAsync(new GotAgvRoute(agvRoute));
    }

    /// <inheritdoc cref="GetAgvRouteRequestHandler"/>
    public async Task Consume(ConsumeContext<GetAgvRoutes> context)
    {
        IReadOnlyDictionary<Guid, AgvRouteReadModel?> agvRoute = await _agvRouteRetrievalService.GetAgvRouteAsync(context.Message.CorrelationIds);
        await context.RespondAsync(new GotAgvRoutes(agvRoute));
    }
}