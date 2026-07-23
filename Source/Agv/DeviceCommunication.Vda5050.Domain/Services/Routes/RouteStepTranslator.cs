using AutoMapper;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Extensions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Agvs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Routes;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Order.OrderAction;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Routes;

/// <summary>
/// Translates a <see cref="RouteExecution"/>'s already-persisted <see cref="RouteStepExecution"/> sequence
/// (layout-agnostic, hashed Guid node/edge ids) into the VDA5050 domain <see cref="Models.Orders.Route"/>/
/// <see cref="OrderNode"/>/<see cref="OrderEdge"/> via a reverse-lookup against
/// <see cref="ILifRepository"/>-provided layout data.
/// </summary>
/// <remarks>
/// Each <see cref="RouteStepExecution"/>'s <c>NodeSequenceId</c>/<c>EdgeSequenceId</c>/<c>VdaActionId</c> are
/// filled in on the passed-in entities as a side effect, so that later state attribution can find them again
/// after an orderId rotation. The wire-level <c>Released</c> flag of each produced node/edge is taken directly
/// from the corresponding step's already-decided <see cref="RouteStepExecution.Released"/> value.
/// </remarks>
public class RouteStepTranslator(
    IAgvRepository agvRepository,
    IAgvActionRepository agvActionRepository,
    ILifRepository lifRepository,
    OrderActionBuilderRegistry orderActionBuilderRegistry,
    RouteActionMapper routeActionMapper,
    IMapper mapper) : IRouteStepTranslator
{
    /// <summary>
    /// Action types that are named by <see cref="RouteStepExecution.ActionAtEndNode"/> and are therefore
    /// handled by <see cref="RouteActionMapper"/>, not by the automatic, graph-driven action logic below.
    /// </summary>
    private static readonly HashSet<string> _routeControlledActionTypes =
    [
        Vda5050Specifications.ActionTypes.Pick,
        Vda5050Specifications.ActionTypes.Drop,
        Vda5050Specifications.ActionTypes.StartCharging
    ];

    /// <inheritdoc/>
    public async Task<Route> Translate(Guid agvCorrelationId, IReadOnlyList<RouteStepExecution> steps)
    {
        var agv = await agvRepository.GetRequiredAgvAsync(agvCorrelationId);

        // Currently, as mapping from lif to fleetmanager ids is based on nodeId/edgeId.ToGuid
        // all nodes and edges of the layouts are selected, and edges/nodes needed resolved using this 
        // relation. Once available, update this part.
        ICollection<Layout> layouts = await lifRepository.GetLayoutsByVehicleTypeIdAsync(agv.VehicleTypeId);
        List<Node> agvNodes = layouts.SelectMany(l => l.Nodes).ToList();
        List<Edge> agvEdges = layouts.SelectMany(l => l.Edges).ToList();
        ICollection<Models.AgvActions.AgvAction> agvNodeActions = await agvActionRepository.GetActionsForScopeAsync(agv.SerialNumber, Models.AgvActions.ActionScope.Node);

        List<OrderNode> orderNodes = [];
        List<OrderEdge> orderEdges = [];

        var seedGraphNode = agvNodes.ResolveNode(steps[0].StartNodeId, agvCorrelationId);
        var seedNode = new OrderNode(seedGraphNode, sequenceId: 0, released: true);
        AddAutomaticNodeActions(agv, seedGraphNode, agvNodeActions, seedNode);
        orderNodes.Add(seedNode);

        var currentNode = seedNode;
        int nextNodeIndex = 1;
        foreach (var step in steps)
        {
            if (step.IsStationaryAction)
            {
                var mergedGraphNode = agvNodes.ResolveNode(step.EndNodeId, agvCorrelationId);
                var action = await routeActionMapper.MapAsync(agv, mergedGraphNode, step.ActionAtEndNode);
                if (action != null)
                {
                    currentNode.Actions.Add(action);
                }
                currentNode.Released = currentNode.Released && step.Released;
                step.NodeSequenceId = currentNode.SequenceId;
                step.EdgeSequenceId = null;
                step.VdaActionId = action?.ActionId;
                continue;
            }

            var endGraphNode = agvNodes.ResolveNode(step.EndNodeId, agvCorrelationId);
            var graphEdge = agvEdges.ResolveEdge(step.EdgeId!.Value, agvCorrelationId);

            var orderEdge = BuildOrderEdge(agv, graphEdge, nextNodeIndex * 2 - 1, step.Released);
            orderEdges.Add(orderEdge);

            var orderNode = new OrderNode(endGraphNode, nextNodeIndex * 2, step.Released);
            AddAutomaticNodeActions(agv, endGraphNode, agvNodeActions, orderNode);
            var endAction = await routeActionMapper.MapAsync(agv, endGraphNode, step.ActionAtEndNode);
            if (endAction != null)
            {
                orderNode.Actions.Add(endAction);
            }
            orderNodes.Add(orderNode);

            step.NodeSequenceId = orderNode.SequenceId;
            step.EdgeSequenceId = orderEdge.SequenceId;
            step.VdaActionId = endAction?.ActionId;

            currentNode = orderNode;
            nextNodeIndex++;
        }

        return new Route(orderNodes, orderEdges);
    }

    private OrderEdge BuildOrderEdge(Agv agv, Edge graphEdge, int sequenceId, bool released)
    {
        var properties = graphEdge.GetPropertiesForVehicle(agv.VehicleTypeId);
        return new OrderEdge
        {
            EdgeId = graphEdge.EdgeId,
            SequenceId = sequenceId,
            Released = released,
            StartNodeId = graphEdge.StartNodeId,
            EndNodeId = graphEdge.EndNodeId,
            MaxHeight = properties?.MaxHeight,
            MaxRotationSpeed = properties?.MaxRotationSpeed,
            MaxSpeed = properties?.MaxSpeed,
            MinHeight = properties?.MinHeight,
            Orientation = properties?.VehicleOrientation,
            OrientationType = mapper.Map<OrientationType?>(properties?.OrientationType),
            RotationAllowed = properties?.RotationAllowed,
            Trajectory = mapper.Map<Models.Orders.Trajectory?>(properties?.Trajectory)
        };
    }

    private void AddAutomaticNodeActions(Agv agv, Node graphNode, ICollection<Models.AgvActions.AgvAction> agvNodeActions, OrderNode orderNode)
    {
        var properties = graphNode.GetPropertiesForVehicle(agv.VehicleTypeId);
        foreach (var requiredAction in properties?.Actions?.Where(a => a.RequirementType is RequirementType.Required or RequirementType.Conditional) ?? [])
        {
            if (_routeControlledActionTypes.Contains(requiredAction.ActionType))
            {
                continue;
            }

            var definedAction = agvNodeActions.SingleOrDefault(a => a.ActionType == requiredAction.ActionType);
            var context = new OrderActionContext(requiredAction.ActionParameters?.Select(p => new OrderActionParameter { Key = p.Key, Value = p.Value }).ToList() ?? [], requiredAction.BlockingType, Vda5050Specifications.ActionTypes.IsVda5050Action(requiredAction.ActionType));
            orderNode.Actions.Add(definedAction != null ? orderActionBuilderRegistry.Build(definedAction, context) : orderActionBuilderRegistry.Build(requiredAction));
        }
    }
}
