
using Gebhardt.Acc.Common.Messages.Route.AgvActions;
using Gebhardt.Acc.TransportOrder.TrafficManagement.Graph.Models;

namespace Gebhardt.Acc.TransportOrder.TrafficManagement.Routes;

using CommonRouteStep = Common.Messages.Route.RouteSteps.RouteStep;

/// <summary>
/// Extension methods for mapping RouteStep.
/// </summary>
internal static class RouteStepExtensions
{
    /// <summary>
    /// Maps a RouteStep to a CommonRouteStep.
    /// </summary>
    public static CommonRouteStep ToMessage(this RouteStep routeStep)
    {
        var routingEdge = routeStep.RoutingEdge;
        var edgeId = (routingEdge.Parent as Edge)?.Id;

        return new CommonRouteStep(
            routeStep.RouteStepId,
            routingEdge.StartNode.Parent.Id,
            edgeId,
            routingEdge.EndNode.Parent.Id, 
            GetAction(routingEdge));
    }

    // ReSharper disable once UnusedParameter.Local
    private static AgvAction? GetAction(RoutingEdge routingEdge)
    {
        //todo: implement later

        return null;
    }
}