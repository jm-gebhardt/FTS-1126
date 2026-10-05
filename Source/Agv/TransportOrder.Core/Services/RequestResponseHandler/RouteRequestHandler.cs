using AutoMapper;
using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Gebhardt.Acc.TransportOrder.Messages.MessageContent;
using Gebhardt.Acc.TransportOrder.Messages.Requests.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Routing;
using Gebhardt.Acc.TransportOrder.Routing.Exceptions;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Provides handlers for <see cref="Route"/> related requests.
/// </summary>
[PublicAPI]
public class RouteRequestHandler : IConsumer<GetRoute>
{
    private readonly IAgvDb _agvDb;
    private readonly IRouteService _routeService;
    private readonly IMapper _mapper;

    /// <inheritdoc cref="RouteRequestHandler"/>
    public RouteRequestHandler(IAgvDb agvDb, IRouteService routeService, IMapper mapper)
    {
        _agvDb = agvDb;
        _routeService = routeService;
        _mapper = mapper;
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
            var modelRoute = await _routeService.GetRouteAsync(agv, getRoute.DestinationNodeId, getRoute.StartNodeId, agvData);

            var nodes = _mapper.Map<GraphNode[]>(modelRoute.Nodes);
            route = new Route(agv.CorrelationId, nodes, modelRoute.Edges);
        }
        catch (RouteNotFoundException)
        {
            route = null;
        }
        catch (NodeNotFoundException e)
        {
            throw new ValidationException(e.Message);
        }
        await context.RespondAsync(new GotRoute(route));
    }
}