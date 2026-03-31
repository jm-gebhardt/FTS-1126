using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Extensions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvActions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Agvs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Graphs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Order.OrderAction;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using OrderActionContext = Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders.OrderActionContext;
using OrderEdge = Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders.OrderEdge;
using OrderNode = Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders.OrderNode;
using OrderNodePosition = Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders.OrderNodePosition;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Order;

/// <summary>
/// Provies a service that builds <see cref="Order"/>s from <see cref="Common.Messages.ActionGroups.AgvAction"/>s
/// </summary>
public class OrderBuilder(
    ILogger<OrderBuilder> logger,
    IAgvRepository agvRepository,
    IAgvActionRepository agvActionRepository,
    IAgvGraphProvider agvGraphProvider,
    OrderActionBuilderRegistry orderActionBuilder) : IOrderBuilder
{
    ///<inheritdoc/>
    public async Task<Models.Orders.Order> Build(Guid agvCorrelationId, Common.Messages.ActionGroups.AgvAction action)
    {
        var agvGraph = await agvGraphProvider.GetRequiredAgvGraphAsync(agvCorrelationId);
        var agv = await agvRepository.GetRequiredAgvAsync(agvCorrelationId);
        
        ICollection<OrderNode> nodes = await BuildNodes(agv, agvGraph.Nodes, action);
        ICollection<OrderEdge> edges = await BuildEdges(agv, agvGraph.Edges, action);
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
    /// <param name="action">The action.</param>
    [PublicAPI]
    public async Task<ICollection<OrderEdge>> BuildEdges(Agv agv, ICollection<Edge> agvEdges, Common.Messages.ActionGroups.AgvAction action)
    {
        var orderEdges = new List<OrderEdge>(); 
        ICollection<AgvAction> agvEdgeActions = await agvActionRepository.GetActionsForScopeAsync(agv.SerialNumber, ActionScope.Edge);
        foreach (var passed in action.Edges.Select((edgeId, index) => new { index, edgeId }))
        {
            var agvGraphEdge = agvEdges.SingleOrDefault(e => e.Id == passed.edgeId);
            if (agvGraphEdge == null)
            {
                logger.LogError("Failed to create order edge {EdgeId} for action {ActionCorrelationId} of agv {AgvCorrelationId}", passed.edgeId, action.CorrelationId, agv.CorrelationId);
                throw new EdgeNotDefinedForAgvException(passed.edgeId, agv.CorrelationId);
            }
            var orderEdge = new OrderEdge
            {
                EdgeId = agvGraphEdge.EdgeId,
                SequenceId = passed.index * 2 + 1, // sequenceId runs over nodes and edges
                Released = false,
                EndNodeId = agvGraphEdge.EndNodeId,
                StartNodeId = agvGraphEdge.StartNodeId,
                MaxHeight = agvGraphEdge.MaxHeight,
                MaxRotationSpeed = agvGraphEdge.MaxRotationSpeed,
                MaxSpeed = agvGraphEdge.MaxSpeed,
                MinHeight = agvGraphEdge.MinHeight,
                Orientation = agvGraphEdge.AgvOrientation,
                OrientationType = agvGraphEdge.OrientationType,
                RotationAllowed = agvGraphEdge.RotationAllowed,
                Trajectory = agvGraphEdge.AgvTrajectory
            };

            foreach (var requiredAction in agvGraphEdge.AgvActions.Where(a => a.RequirementType is RequirementType.Required or RequirementType.Conditional))
            {
                var definedAction = agvEdgeActions.SingleOrDefault(a => a.ActionType == requiredAction.ActionType);
                var context = new OrderActionContext(requiredAction.Parameters, requiredAction.BlockingType, Vda5050Specifications.ActionTypes.IsVda5050Action(requiredAction.ActionType));
                if (definedAction != null)
                {
                    logger.LogInformation("Building required action {ActionType} on node {NodeId} for action {ActionCorrelationId} of agv {AgvCorrelationId} using fact sheet and graph information", requiredAction.ActionType, passed.edgeId, action.CorrelationId, agv.CorrelationId);
                    orderEdge.Actions.Add(orderActionBuilder.Build(definedAction, context));
                    continue;
                }
                logger.LogInformation("Building required action {ActionType} on node {NodeId} for action {ActionCorrelationId} of agv {AgvCorrelationId} using only graph information", requiredAction.ActionType, passed.edgeId, action.CorrelationId, agv.CorrelationId);
                orderEdge.Actions.Add(orderActionBuilder.Build(requiredAction));
            }
            foreach (var optionalAction in agvGraphEdge.AgvActions.Where(a => a.RequirementType is RequirementType.Optional))
            {
                // Conditions for running optional action have to be specified, for now only log
                logger.LogWarning("Not building optional action {ActionType} on edge {EdgeId} for action {ActionCorrelationId} of agv {AgvCorrelationId}", optionalAction.ActionType, passed.edgeId, action.CorrelationId, agv.CorrelationId);
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
    /// <param name="action">The ids of the nodes to pass.</param>
    [PublicAPI]
    public async Task<ICollection<OrderNode>> BuildNodes(Agv agv, ICollection<Node> agvNodes, Common.Messages.ActionGroups.AgvAction action)
    {
        var orderNodes = new List<OrderNode>();
        ICollection<AgvAction> agvNodesActions = await agvActionRepository.GetActionsForScopeAsync(agv.SerialNumber, ActionScope.Node);
        foreach (var passed in action.Nodes.Select((node,index) => new {index, node}))
        {
            var agvGraphNode = agvNodes.SingleOrDefault(n => n.Id == passed.node.Id);
            if (agvGraphNode == null)
            {
                logger.LogError("Failed to create order node {NodeId} for action {ActionCorrelationId} of agv {AgvCorrelationId}", passed.node.Id, action.CorrelationId, agv.CorrelationId);
                throw new NodeNotDefinedForAgvException(passed.node.Id, agv.CorrelationId);
            }
            var orderNode = new OrderNode();
            AddNodeOrderActions(agv, action, agvGraphNode, agvNodesActions, passed.node, orderNode);
            orderNode.NodePosition = new OrderNodePosition() { X = agvGraphNode.Position.X, Y = agvGraphNode.Position.Y };
            orderNode.Released = false;
            orderNode.SequenceId = passed.index * 2; // sequenceId runs over nodes and edges
            orderNode.NodeId = agvGraphNode.NodeId;
            orderNodes.Add(orderNode);
        }
        return orderNodes;
    }

    private void AddNodeOrderActions(Agv agv, Common.Messages.ActionGroups.AgvAction action, Node agvGraphNode, ICollection<AgvAction> agvNodesActions, GraphNode passedNode, OrderNode orderNode)
    {
        foreach (var requiredAction in agvGraphNode.AgvActions.Where(a => a.RequirementType is RequirementType.Required or RequirementType.Conditional))
        {
            orderNode.Actions.Add(BuildOrderNodeAction(agv, action, agvNodesActions, requiredAction, passedNode));
        }
        foreach (var optionalAction in agvGraphNode.AgvActions.Where(a => a.RequirementType is RequirementType.Optional))
        {
            if (action.ActionType.MatchesVda5050Type(optionalAction.ActionType))
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

    private Models.Orders.OrderAction BuildOrderNodeAction(Agv agv, Common.Messages.ActionGroups.AgvAction action, ICollection<AgvAction> agvNodesActions, AgvNodeAction requiredAction, GraphNode passedNode)
    {
        var definedAction = agvNodesActions.SingleOrDefault(a => a.ActionType == requiredAction.ActionType);
        var context = new OrderActionContext(requiredAction.Parameters, requiredAction.BlockingType, Vda5050Specifications.ActionTypes.IsVda5050Action(requiredAction.ActionType));
        if (definedAction != null)
        {
            logger.LogInformation("Building required action {ActionType} on node {NodeId} for action {ActionCorrelationId} of agv {AgvCorrelationId} using fact sheet and graph information", requiredAction.ActionType, passedNode.Id, action.CorrelationId, agv.CorrelationId);
            return orderActionBuilder.Build(definedAction, context);
        }
        logger.LogInformation("Building required action {ActionType} on node {NodeId} for action {ActionCorrelationId} of agv {AgvCorrelationId} using only graph information", requiredAction.ActionType, passedNode.Id, action.CorrelationId, agv.CorrelationId);
        return  orderActionBuilder.Build(requiredAction);
    }
}