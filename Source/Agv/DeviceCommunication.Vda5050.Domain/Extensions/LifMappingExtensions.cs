using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Services;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;
using LifTrajectory = Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif.Trajectory;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Extensions;

/// <summary>
/// Extension methods for converting LIF layout models to routing graph models.
/// </summary>
[PublicAPI]
public static class LifMappingExtensions
{
    extension(LifLayoutCollection lifLayoutCollection)
    {
        /// <summary>
        /// Collects all distinct vehicle type Ids across all layouts in the collection.
        /// </summary>
        public HashSet<string> GetVehicleTypeIds()
        {
            IEnumerable<string> nodeIds = lifLayoutCollection.Layouts
                .SelectMany(l => l.Nodes)
                .SelectMany(n => n.VehicleTypeNodeProperties)
                .Select(p => p.VehicleTypeId);

            IEnumerable<string> edgeIds = lifLayoutCollection.Layouts
                .SelectMany(l => l.Edges)
                .SelectMany(e => e.VehicleTypeEdgeProperties)
                .Select(p => p.VehicleTypeId);

            return nodeIds.Concat(edgeIds).ToHashSet();
        }
    }

    extension(ICollection<Layout> layouts)
    {
        /// <summary>
        /// Projects the layouts into a <see cref="Graph"/> filtered to the given vehicle type.
        /// </summary>
        /// <param name="vehicleTypeId">Vehicle type to include in the graph.</param>
        /// <param name="edgeBuilder">Edge builder for nurbs to graph edge conversion.</param>
        public Graph ToGraph(string vehicleTypeId, INurbsEdgeBuilder edgeBuilder)
        {
            return layouts.ToAgvGraph(Guid.Empty, vehicleTypeId, edgeBuilder).ToGraph();
        }

        /// <summary>
        /// Projects the layouts into a vehicle-type-specific <see cref="AgvGraph"/>,
        /// applying trajectory-based edge geometry and inferring node types from actions.
        /// </summary>
        /// <param name="agvCorrelationId">Correlation Id of the AGV the graph is built for.</param>
        /// <param name="vehicleTypeId">Vehicle type to include in the graph.</param>
        /// <param name="edgeBuilder">Edge builder for nurbs to graph edge conversion.</param>
        public AgvGraph ToAgvGraph(Guid agvCorrelationId, string vehicleTypeId, INurbsEdgeBuilder edgeBuilder)
        {
            List<GraphNode> nodes = BuildNodes(layouts, vehicleTypeId);
            List<GraphEdge> edges = BuildEdges(layouts, vehicleTypeId, nodes, edgeBuilder);
            return new AgvGraph(agvCorrelationId, nodes, edges);
        }
    }

    extension(LifTrajectory? trajectory)
    {
        /// <summary>
        /// Converts a <see cref="LifTrajectory"/> to a <see cref="Nurbs"/> curve.
        /// Returns <see langword="null"/> when the trajectory is absent or has no control points.
        /// </summary>
        public Nurbs? ToNurbs()
        {
            if (trajectory == null || trajectory.ControlPoints.Count == 0)
            {
                return null;
            }

            return new Nurbs
            {
                Degree = (int)(trajectory.Degree ?? 1), // default degree given by the LIF specification
                KnotVector = trajectory.KnotVector,
                ControlPoints = trajectory.ControlPoints
                    .Select(cp => new NurbsControlPoint(cp.X, cp.Y, cp.Weight))
                    .ToArray()
            };
        }
    }

    extension(ICollection<NodeActionParameter>? parameters)
    {
        /// <summary>
        /// Converts node action parameters to <see cref="OrderActionParameter"/> instances.
        /// </summary>
        public ICollection<OrderActionParameter> ToOrderParameters()
        {
            if (parameters == null || parameters.Count == 0)
            {
                return [];
            }

            return [.. parameters.Select(p => new OrderActionParameter { Key = p.Key, Value = p.Value })];
        }
    }

    extension(ICollection<EdgeActionParameter>? parameters)
    {
        /// <summary>
        /// Converts edge action parameters to <see cref="OrderActionParameter"/> instances.
        /// </summary>
        public ICollection<OrderActionParameter> ToOrderParameters()
        {
            if (parameters == null || parameters.Count == 0)
            {
                return [];
            }

            return [.. parameters.Select(p => new OrderActionParameter { Key = p.Key, Value = p.Value })];
        }
    }

    private static List<GraphNode> BuildNodes(ICollection<Layout> layouts, string vehicleTypeId)
    {
        var nodes = new List<GraphNode>();

        foreach (var layout in layouts)
        {
            foreach (var node in layout.Nodes)
            {
                var vehicleProps = node.GetPropertiesForVehicle(vehicleTypeId);
                if (vehicleProps == null)
                {
                    continue;
                }

                float theta = (float)(vehicleProps.Theta ?? ResolveStationTheta(layout, node.NodeId));

                var graphNode = new GraphNode(node.GetFleetManagerNodeIdForVehicle(vehicleTypeId) ?? node.NodeId.ToGuid(), node.NodeId)
                {
                    X = (float)node.NodePosition.X,
                    Y = (float)node.NodePosition.Y,
                    Z = 0,
                    Theta = theta,
                    TimeStamp = DateTime.UtcNow 
                };

                if (vehicleProps.Actions is { Count: > 0 } actions)
                {
                    ApplyNodeType(graphNode, actions);
                }

                nodes.Add(graphNode);
            }
        }

        return nodes;
    }

    private static List<GraphEdge> BuildEdges(
        ICollection<Layout> layouts,
        string vehicleTypeId,
        IReadOnlyCollection<GraphNode> nodes, INurbsEdgeBuilder edgeBuilder)
    {
        Dictionary<Guid, GraphNode> nodeById = nodes.ToDictionary(n => n.Id);
        var edges = new List<GraphEdge>();

        foreach (var layout in layouts)
        {
            foreach (var edge in layout.Edges)
            {
                var vehicleProps = edge.GetPropertiesForVehicle(vehicleTypeId);
                if (vehicleProps == null)
                {
                    continue;
                }
                var layoutStartNode = layout.Nodes.SingleOrDefault(n => n.NodeId == edge.StartNodeId);
                Guid? startId = layoutStartNode?.GetFleetManagerNodeIdForVehicle(vehicleTypeId);
                if (startId == null)
                {
                    continue;
                }
                var layoutEndNode = layout.Nodes.SingleOrDefault(n => n.NodeId == edge.EndNodeId);
                Guid? endId = layoutEndNode?.GetFleetManagerNodeIdForVehicle(vehicleTypeId);
                if (endId == null)
                {
                    continue;
                }
                if (!nodeById.TryGetValue(startId.Value, out  _) ||
                    !nodeById.TryGetValue(endId.Value, out _))
                {
                    continue;
                }
                Guid? id = edge.GetFleetManagerEdgeIdForVehicle(vehicleTypeId);
                if (id == null)
                {
                    continue;
                }
                if(vehicleProps.Trajectory == null)
                {
                    edges.Add(GraphEdge.CreateDirect(startId.Value, endId.Value, (float)Mathematics.EuclideanDistance(nodeById[startId.Value], nodeById[endId.Value]), id, DateTime.UtcNow));
                    continue;
                }
                var nurbs = vehicleProps.Trajectory.ToNurbs();
                if (nurbs == null)
                {
                    continue;
                }
                edges.Add(edgeBuilder.BuildFromNurbs(nurbs, startId.Value, endId.Value, id, DateTime.UtcNow));
            }
        }

        return edges;
    }

    private static void ApplyNodeType(GraphNode node, ICollection<VehicleTypeNodePropertyAction> actions)
    {
        HashSet<string> actionTypes = actions.Select(a => a.ActionType).ToHashSet();
        bool hasPick = actionTypes.Contains(Vda5050Specifications.ActionTypes.Pick);
        bool hasDrop = actionTypes.Contains(Vda5050Specifications.ActionTypes.Drop);
        bool hasCharge = actionTypes.Contains(Vda5050Specifications.ActionTypes.StartCharging);

        var nodeTypes = new List<AgvGraph.NodeType>();

        if (hasPick && hasDrop)
        {
            nodeTypes.Add(AgvGraph.NodeType.PickDrop);
        }
        else if (hasPick)
        {
            nodeTypes.Add(AgvGraph.NodeType.Pick);
        }
        else if (hasDrop)
        {
            nodeTypes.Add(AgvGraph.NodeType.Drop);
        }

        if (hasCharge)
        {
            nodeTypes.Add(AgvGraph.NodeType.Charge);
        }

        if (nodeTypes.Count > 0)
        {
            node.UpdateType(nodeTypes);
        }
    }

    private static double ResolveStationTheta(Layout layout, string nodeId)
    {
        return layout.Stations
            .Where(s => s.InteractionNodeIds.Contains(nodeId))
            .Select(s => s.StationPosition?.Theta)
            .FirstOrDefault(t => t.HasValue) ?? 0;
    }
}
