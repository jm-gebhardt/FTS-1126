using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Routing;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Routing;
using Gebhardt.StoreWare.Agv.TransportOrder.Routing.Exceptions;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Provides handlers for <see cref="Route"/> related requests.
/// </summary>
[PublicAPI]
public class RouteRequestHandler : IConsumer<GetRoute>
{
    private readonly IAgvRepository _agvRepository;
    private readonly IRouteService _routeService;

    /// <inheritdoc cref="RouteRequestHandler"/>
    /// <param name="agvRepository">The agv repository.</param>
    /// <param name="routeService">The route service.</param>
    public RouteRequestHandler(IAgvRepository agvRepository, IRouteService routeService)
    {
        _agvRepository = agvRepository;
        _routeService = routeService;
    }

    /// <summary>
    /// Handles the  <see cref="GetActionGroup"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<GetRoute> context)
    {
        var getRoute = context.Message;
        var agv = await _agvRepository.GetAgvAsync(getRoute.CorrelationId);
        Route? route;
        try
        {
            string[] nodes = (await _routeService.GetRouteAsync(agv, getRoute.DestinationNodeId, getRoute.StartNodeId)).Nodes;
            route = new Route(agv.CorrelationId, nodes);
        }
        catch (RouteNotFoundException)
        {
            route = null;
        }
        catch (NodeNotFoundException e)
        {
            throw new BadRequestException(e.Message);
        }
        await context.RespondAsync(new GotRoute(route));
    }
}