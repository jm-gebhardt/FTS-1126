using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Extensions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvActions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Agvs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Order.OrderAction;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using OrderActionContext = Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders.OrderActionContext;
using OrderEdge = Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders.OrderEdge;
using OrderNode = Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders.OrderNode;
using OrderNodePosition = Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders.OrderNodePosition;
using RoutePlan = Gebhardt.Acc.Common.Messages.ActionGroups.RoutePlan;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Order;

/// <summary>
/// Provies a service that builds <see cref="Order"/>s from <see cref="Common.Messages.ActionGroups.AgvAction"/>s
/// </summary>
public class OrderBuilder(
    ILogger<OrderBuilder> logger,
    IAgvRepository agvRepository,
    IAgvActionRepository agvActionRepository,
    ILifRepository lifRepository,
    OrderActionBuilderRegistry orderActionBuilder,
    IMapper mapper) : IOrderBuilder
{
    /// <inheritdoc/>
    public async Task<Models.Orders.Order> Build(Guid agvCorrelationId, RoutePlan routePlan, Common.Messages.ActionGroups.AgvAction action)
    {
        var agv = await agvRepository.GetRequiredAgvAsync(agvCorrelationId);
        ICollection<Layout> layouts = await lifRepository.GetLayoutsByVehicleTypeIdAsync(agv.VehicleTypeId);

        ICollection<OrderNode> nodes = await BuildNodes(agv, layouts.SelectMany(n => n.Nodes).ToList(), action, routePlan.BaseNodes);
        ICollection<OrderEdge> edges = await BuildEdges(agv, layouts.SelectMany(n => n.Edges).ToList(), routePlan.BaseEdges, action.CorrelationId);
        var plannedRoute = new Route(nodes, edges);
        if (!plannedRoute.IsValid)
        {
            throw new OrderNodeAndEdgeMismatchException(edges.Count, nodes.Count, action.CorrelationId, agvCorrelationId);
        }

        var order = new Models.Orders.Order()
        {
            OrderId = action.CorrelationId.ToString(),
            OrderUpdateId = 0,
            AssignedAgv = agv,
            PlannedRoute = plannedRoute,
            Route = new Route([], [])
        };

        return order;
    }

    /// <summary>
    /// Build order edges for the given agv for given edges.
    /// </summary>
    /// <param name="agv">The agv.</param>
    /// <param name="agvEdges">The edges from the agv graph definition.</param>
    /// <param name="baseEdges">The route edges.</param>
    /// <param name="actionCorrelationId">The correlation id of the action.</param>
    [PublicAPI]
    public async Task<ICollection<OrderEdge>> BuildEdges(Agv agv, ICollection<Edge> agvEdges, IReadOnlyList<GraphEdge> baseEdges, Guid actionCorrelationId)
    {
        var orderEdges = new List<OrderEdge>();
        ICollection<AgvAction> agvEdgeActions = await agvActionRepository.GetActionsForScopeAsync(agv.SerialNumber, ActionScope.Edge);
        foreach (var passed in baseEdges.Select((edge, index) => new { index, edge }))
        {
            var agvGraphEdge = agvEdges.SingleOrDefault(e => e.EdgeId.ToGuid() == passed.edge.Id);
            if (agvGraphEdge == null)
            {
                logger.LogError("Failed to create order edge {EdgeId} for action {ActionCorrelationId} of agv {AgvCorrelationId}", passed.edge.Id, actionCorrelationId, agv.CorrelationId);
                throw new EdgeNotDefinedForAgvException(passed.edge.Id, agv.CorrelationId);
            }
            var properties = agvGraphEdge.GetPropertiesForVehicle(agv.VehicleTypeId);
            var orderEdge = new OrderEdge
            {
                EdgeId = agvGraphEdge.EdgeId,
                SequenceId = passed.index * 2 + 1, // sequenceId runs over nodes and edges
                Released = false,
                EndNodeId = agvGraphEdge.EndNodeId,
                StartNodeId = agvGraphEdge.StartNodeId,
                MaxHeight = properties?.MaxHeight,
                MaxRotationSpeed = properties?.MaxRotationSpeed,
                MaxSpeed = properties?.MaxSpeed,
                MinHeight = properties?.MinHeight,
                Orientation = properties?.VehicleOrientation,
                OrientationType = mapper.Map<OrientationType?>(properties?.OrientationType),
                RotationAllowed = properties?.RotationAllowed,
                Trajectory = mapper.Map<Models.Orders.Trajectory?>(properties?.Trajectory)
            };

            foreach (var requiredAction in properties?.Actions?.Where(a => a.RequirementType is RequirementType.Required or RequirementType.Conditional) ?? [])
            {
                var definedAction = agvEdgeActions.SingleOrDefault(a => a.ActionType == requiredAction.ActionType);
                var context = new OrderActionContext(requiredAction.ActionParameters?.Select(p => new OrderActionParameter() { Key=p.Key, Value =p.Value}).ToList() ?? [], requiredAction.BlockingType, Vda5050Specifications.ActionTypes.IsVda5050Action(requiredAction.ActionType));
                if (definedAction != null)
                {
                    logger.LogInformation("Building required action {ActionType} on node {NodeId} for action {ActionCorrelationId} of agv {AgvCorrelationId} using factsheet and graph information", requiredAction.ActionType, passed.edge.Id, actionCorrelationId, agv.CorrelationId);
                    orderEdge.Actions.Add(orderActionBuilder.Build(definedAction, context));
                    continue;
                }
                logger.LogInformation("Building required action {ActionType} on node {NodeId} for action {ActionCorrelationId} of agv {AgvCorrelationId} using only graph information", requiredAction.ActionType, passed.edge.Id, actionCorrelationId, agv.CorrelationId);
                orderEdge.Actions.Add(orderActionBuilder.Build(requiredAction));
            }
            foreach (var optionalAction in properties?.Actions?.Where(a => a.RequirementType is RequirementType.Optional) ?? [])
            {
                // Conditions for running optional action have to be specified, for now only log
                logger.LogWarning("Not building optional action {ActionType} on edge {EdgeId} for action {ActionCorrelationId} of agv {AgvCorrelationId}", optionalAction.ActionType, passed.edge.Id, actionCorrelationId, agv.CorrelationId);
            }
            orderEdges.Add(orderEdge);
        }
        return orderEdges;
    }

    /// <summary>
    /// Build order nodes for the given agv for given edges.
    /// </summary>
    /// <param name="agv">The agv.</param>
    /// <param name="agvNodes">The nodes of the agv graph definition.</param>
    /// <param name="action">The action.</param>
    /// <param name="baseNodes">The route nodes.</param>
    [PublicAPI]
    public async Task<ICollection<OrderNode>> BuildNodes(Agv agv, ICollection<Node> agvNodes, Common.Messages.ActionGroups.AgvAction action, IReadOnlyList<GraphNode> baseNodes)
    {
        var orderNodes = new List<OrderNode>();
        ICollection<AgvAction> agvNodesActions = await agvActionRepository.GetActionsForScopeAsync(agv.SerialNumber, ActionScope.Node);
        foreach (var passed in baseNodes.Select((node, index) => new { index, node }))
        {
            var agvGraphNode = agvNodes.SingleOrDefault(n => n.NodeId.ToGuid() == passed.node.Id);
            if (agvGraphNode == null)
            {
                logger.LogError("Failed to create order node {NodeId} for action {ActionCorrelationId} of agv {AgvCorrelationId}", passed.node.Id, action.CorrelationId, agv.CorrelationId);
                throw new NodeNotDefinedForAgvException(passed.node.Id, agv.CorrelationId);
            }
            var orderNode = new OrderNode();
            AddNodeOrderActions(agv, action, agvGraphNode, agvNodesActions, passed.node, orderNode);
            orderNode.NodePosition = new OrderNodePosition() { X = agvGraphNode.NodePosition.X, Y = agvGraphNode.NodePosition.Y };
            orderNode.Released = false;
            orderNode.SequenceId = passed.index * 2; // sequenceId runs over nodes and edges
            orderNode.NodeId = agvGraphNode.NodeId;
            orderNodes.Add(orderNode);
        }
        return orderNodes;
    }

    private void AddNodeOrderActions(Agv agv, Common.Messages.ActionGroups.AgvAction action, Node agvGraphNode, ICollection<AgvAction> agvNodesActions, GraphNode passedNode, OrderNode orderNode)
    {
        var properties = agvGraphNode.GetPropertiesForVehicle(agv.VehicleTypeId);

        foreach (var requiredAction in properties?.Actions?.Where(a => a.RequirementType is RequirementType.Required or RequirementType.Conditional) ?? [])
        {
            if (Vda5050Specifications.ActionTypes.IsVda5050Action(requiredAction.ActionType)
                && (!action.ActionType.MatchesVda5050Type(requiredAction.ActionType) || action.Target.Node?.Id != passedNode.Id))
            {
                continue;
            }
            orderNode.Actions.Add(BuildOrderNodeAction(agv, action, agvNodesActions, requiredAction, passedNode));
        }
        foreach (var optionalAction in properties?.Actions?.Where(a => a.RequirementType is RequirementType.Optional) ?? [])
        {
            if (action.ActionType.MatchesVda5050Type(optionalAction.ActionType) && action.Target.Node?.Id == passedNode.Id)
            {
                // E.g. add optional charge action only if the action type is 'Charge', not in case it is 'Move'
                orderNode.Actions.Add(BuildOrderNodeAction(agv, action, agvNodesActions, optionalAction, passedNode));
                continue;
            }
            if (!Vda5050Specifications.ActionTypes.IsVda5050Action(optionalAction.ActionType))
            {
                // Conditions for running optional custom actions from manufacturer are run have to be specified, for now only log
                logger.LogWarning("Not building optional action {ActionType} on node {NodeId} for action {ActionCorrelationId} of agv {AgvCorrelationId}", optionalAction.ActionType, passedNode.Id, action.CorrelationId, agv.CorrelationId);
            }
        }
    }

    private Models.Orders.OrderAction BuildOrderNodeAction(Agv agv, Common.Messages.ActionGroups.AgvAction action, ICollection<AgvAction> agvNodesActions, VehicleTypeNodePropertyAction requiredAction, GraphNode passedNode)
    {
        var definedAction = agvNodesActions.SingleOrDefault(a => a.ActionType == requiredAction.ActionType);
        var context = new OrderActionContext(requiredAction.ActionParameters?.Select(p => new OrderActionParameter { Key=p.Key, Value=p.Value}).ToList() ?? [], requiredAction.BlockingType, Vda5050Specifications.ActionTypes.IsVda5050Action(requiredAction.ActionType));
        if (definedAction != null)
        {
            logger.LogInformation("Building required action {ActionType} on node {NodeId} for action {ActionCorrelationId} of agv {AgvCorrelationId} using factsheet and graph information", requiredAction.ActionType, passedNode.Id, action.CorrelationId, agv.CorrelationId);
            return orderActionBuilder.Build(definedAction, context);
        }
        logger.LogInformation("Building required action {ActionType} on node {NodeId} for action {ActionCorrelationId} of agv {AgvCorrelationId} using only graph information", requiredAction.ActionType, passedNode.Id, action.CorrelationId, agv.CorrelationId);
        return orderActionBuilder.Build(requiredAction);
    }
}