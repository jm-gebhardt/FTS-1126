using Gebhardt.Acc.TransportOrder.TrafficManagement.Graph.Models;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.TrafficManagement.Routes.Generation;

/// <summary>
/// Generates routes for AGVs using an advanced A* search algorithm.
/// </summary>
[UsedImplicitly]
internal class RouteGenerator(ILogger<RouteGenerator> logger)
{
    /// <summary>
    /// Speed assumed for edges without an explicit speed limit in [m/s].
    /// </summary>
    private const double DefaultSpeed = 1.5;   //todo: wire to AGV type

    /// <summary>
    /// Upper bound of the speed of any edge in [m/s]. Used by the heuristic to never overestimate the remaining driving time.
    /// </summary>
    private const double MaxAssumedSpeed = 2.5;  //todo: wire to AGV type

    /// <summary>
    /// Generates a route from the start node to one of the target node candidates using the A* search algorithm.
    /// </summary>
    public Route? CreateRoute(RouteCreationRequest request)
    {
        if (!request.IsValid())
        {
            logger.LogWarning("AGV {ActionCorrelationId} invalid route creation request", request.ActionCorrelationId);
            return null;
        }

        var startSearchNode = new SearchNode(null, null, request.StartNode, 0.0, EstimateRemainingCost(request.StartNode, request.TargetNodeCandidates));

        var queue = new SearchNodeQueue();
        queue.TryEnqueue(startSearchNode);

        while (queue.TryDequeue(out var searchNode))
        {
            var currentRoutingNode = searchNode.Node;

            if (request.TargetNodeCandidates.Contains(currentRoutingNode))
            {
                var route = CreateRoute(request, BuildSteps(searchNode));
                return route;
            }

            foreach (var edge in currentRoutingNode.OutgoingEdges)
            {
                var successorNode = edge.EndNode;
                if (!IsTraversable(edge, request))
                {
                    continue;
                }

                double cost = searchNode.CurrentCost + GetCost(edge);
                double totalCost = cost + EstimateRemainingCost(successorNode, request.TargetNodeCandidates);

                queue.TryEnqueue(new SearchNode(searchNode, edge, successorNode, cost, totalCost));
            }
        }

        logger.LogWarning("AGV {ActionCorrelationId} no route found from {StartNode} to any of the target nodes {TargetNodes} (EnqueuedCount: {EnqueuedCount})", request.ActionCorrelationId, request.StartNode, request.TargetNodeCandidates, queue.EnqueuedCount);
        return null;
    }

    private static Route CreateRoute(RouteCreationRequest request, List<RouteStep> steps)
    {
        return new Route(request.Agv, steps, request.ActionCorrelationId);
    }

    private static List<RouteStep> BuildSteps(SearchNode destinationSearchNode)
    {
        var steps = new List<RouteStep>();

        var node = destinationSearchNode;
        while (node.IncomingEdge is not null)
        {
            var edge = node.IncomingEdge;
            steps.Add(new RouteStep(edge, GetDrivingTime(edge), GetCost(edge)));
            node = node.Predecessor!;
        }

        steps.Reverse();
        return steps;
    }

    private static bool IsTraversable(RoutingEdge edge, RouteCreationRequest routeCreationRequest)
    {
        if (edge is ActionRoutingEdge && !routeCreationRequest.WhitelistedActionEdges.Contains(edge))
        {
            return false;
        }

        return true;
    }

    private static double GetCost(RoutingEdge edge)
    {
        return GetDrivingTime(edge);
    }

    private static double GetDrivingTime(RoutingEdge edge)
    {
        double speed = edge.MaxSpeed is > 0.0 ? edge.MaxSpeed.Value : DefaultSpeed;
        return edge.Length / speed;
    }

    /// <summary>
    /// Admissible heuristic: the straight line distance to the closest target candidate, travelled at the highest assumed speed.
    /// </summary>
    private static double EstimateRemainingCost(RoutingNode currentNode, IEnumerable<RoutingNode> targetNodes)
    {
        double minimumDistance = double.MaxValue;
        foreach (var targetNode in targetNodes)
        {
            double distance = Math.Sqrt(Math.Pow(targetNode.ParentNode.X - currentNode.ParentNode.X, 2) + Math.Pow(targetNode.ParentNode.Y - currentNode.ParentNode.Y, 2));
            minimumDistance = Math.Min(minimumDistance, distance);
        }

        return minimumDistance is double.MaxValue ? 0.0 : minimumDistance / MaxAssumedSpeed;
    }
}