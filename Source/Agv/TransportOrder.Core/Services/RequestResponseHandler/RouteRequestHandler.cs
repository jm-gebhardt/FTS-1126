using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
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
    private readonly IAgvDb _agvDb;
    private readonly IRouteService _routeService;

    /// <inheritdoc cref="RouteRequestHandler"/>
    public RouteRequestHandler(IAgvDb agvDb, IRouteService routeService)
    {
        _agvDb = agvDb;
        _routeService = routeService;
    }

    /// <summary>
    /// Handles the  <see cref="GetActionGroup"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<GetRoute> context)
    {
        var getRoute = context.Message;
        var agv = await _agvDb.Agvs.GetAgvAsync(getRoute.CorrelationId);
        var agvData = await _agvDb.Data.GetAgvDataAsync(agv.CorrelationId);
        Route? route;
        try
        {
            string[] nodes = (await _routeService.GetRouteAsync(agv, getRoute.DestinationNodeId, getRoute.StartNodeId, agvData)).Nodes;
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