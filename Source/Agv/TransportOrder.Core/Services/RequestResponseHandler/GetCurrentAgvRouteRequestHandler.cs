using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Routing;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Defines a handler for GetAgvRoute request
/// </summary>
[UsedImplicitly]
public class GetCurrentAgvRouteRequestHandler : IConsumer<GetCurrentAgvRoute>, IConsumer<GetCurrentAgvRoutes>
{
    private readonly IAgvRouteRetrievalService _agvRouteRetrievalService;

    /// <inheritdoc cref="GetCurrentAgvRouteRequestHandler"/>
    public GetCurrentAgvRouteRequestHandler(IAgvRouteRetrievalService agvRouteRetrievalService)
    {
        _agvRouteRetrievalService = agvRouteRetrievalService;
    }

    /// <inheritdoc cref="GetCurrentAgvRouteRequestHandler"/>
    public async Task Consume(ConsumeContext<GetCurrentAgvRoute> context)
    {
        var agvRoute = await _agvRouteRetrievalService.GetAgvRouteAsync(context.Message.CorrelationId);
        await context.RespondAsync(new GotCurrentAgvRoute(agvRoute));
    }

    /// <inheritdoc cref="GetCurrentAgvRouteRequestHandler"/>
    public async Task Consume(ConsumeContext<GetCurrentAgvRoutes> context)
    {
        IReadOnlyDictionary<Guid, AgvRouteReadModel?> agvRoute = await _agvRouteRetrievalService.GetAgvRouteAsync(context.Message.CorrelationIds);
        await context.RespondAsync(new GotCurrentAgvRoutes(agvRoute));
    }
}