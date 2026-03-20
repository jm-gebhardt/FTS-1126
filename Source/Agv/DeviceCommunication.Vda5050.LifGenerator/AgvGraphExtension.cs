using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Extensions;
using Vdma.Lif;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.LifGenerator;

/// <summary>
/// Provides extension methods for <see cref="AgvGraph"/>
/// </summary>
public static class AgvGraphExtension
{
    extension(AgvGraph graph)
    {
        /// <summary>
        /// Converts an <see cref="AgvGraph"/> to <see cref="Layout"/>
        /// </summary>
        public Layout ToLifLayout()
        {
            Node[] nodes = graph.Nodes.Select(n => n.ToLifNode()).ToArray();
            Edge[] edges = graph.Edges.Select(e => e.ToLifEdge(graph.Nodes.Single(n => n.Id == e.FromNodeId), graph.Nodes.Single(n => n.Id == e.ToNodeId))).ToArray();
            Station[] stations = graph.Nodes.Where(n => n.IsStation()).Select(n => n.ToLifStation()).ToArray();

            return new Layout()
            {
                LayoutId = "Layout-001",
                LayoutName = "Primary layout for testing",
                LayoutVersion = "0.1",
                LayoutDescription = "Layout generated from agv graph",
                LayoutLevelId = "level -1 ",
                Edges = edges,
                Nodes = nodes,
                Stations = stations
            };
        }
    }

    extension(GraphEdge edge)
    {
        /// <summary>
        ///  Converts a <see cref="GraphEdge"/> to <see cref="Edge"/>
        /// </summary>
        /// <param name="fromNode">The source node.</param>
        /// <param name="toNode">The target node.</param>
        public Edge ToLifEdge(GraphNode fromNode, GraphNode toNode)
        {
            return new Edge()
            {
                VehicleTypeEdgeProperties =
                [
                    new VehicleTypeEdgeProperty()
                    {
                        RotationAllowed = true, ReentryAllowed = true, MaxSpeed = 10, OrientationType = "TANGENTIAL",
                        VehicleOrientation = 0.0, Trajectory = edge.GetTrajectory(fromNode, toNode),
                        VehicleTypeId = "rikeb.series"
                    }
                ],
                StartNodeId = fromNode.Id.ToString(),
                EndNodeId = toNode.Id.ToString(),
                EdgeId = edge.Id.ToString()
            };
        }

        /// <summary>
        /// Get an edge trajectory
        /// </summary>
        /// <param name="fromNode">Source node</param>
        /// <param name="toNode">Target node</param>
        /// <returns></returns>
        public Trajectory GetTrajectory(GraphNode fromNode, GraphNode toNode)
        {
            if (new List<EdgeType> { EdgeType.Bezier2, EdgeType.Bezier3, EdgeType.Bezier5 }.Contains(edge.Variant))
            {
                return new Trajectory()
                {
                    Degree = edge.VariantParameters.Count + 1,
                    ControlPoints = [new ControlPoint { X = fromNode.X, Y = fromNode.Y, Weight = 1 }, ..edge.VariantParameters.Select(v => new ControlPoint() { X = v.X!.Value, Y = v.Y!.Value, Weight = 1 }), new ControlPoint { X = toNode.X, Y = toNode.Y, Weight = 1 }],
                    KnotVector = []
                };
            }
            if (edge.Variant == EdgeType.Direct)
            {
                return new Trajectory()
                {
                    Degree = 1,
                    ControlPoints = [new ControlPoint { X = fromNode.X, Y = fromNode.Y, Weight = 1 }, new ControlPoint { X = toNode.X, Y = toNode.Y, Weight = 1 }],
                    KnotVector = [0.0, 0.0, 1.0, 1.0]
                };
            }
            throw new Exception($"edge type {edge.Variant} can not be converted to trajectory");
        }
    }

    extension(GraphNode node)
    {
        /// <summary>
        /// Converts an <see cref="GraphNode"/> to <see cref="Station"/>
        /// </summary>
        public Station ToLifStation()
        {
            return new Station()
            {
                InteractionNodeIds = [node.Id.ToString()],
                StationId = node.Name.ToGuid().ToString(),
                StationName = node.Name,
                StationPosition = new StationPosition() { Theta = node.Theta, X = node.X, Y = node.Y }
            };
        }

        /// <summary>
        /// Converts an <see cref="GraphNode"/> to <see cref="Node"/>
        /// </summary>
        public Node ToLifNode()
        {
            return new Node()
            {
                NodeId = node.Id.ToString(),
                NodeName = node.Name,
                MapId = "map",
                VehicleTypeNodeProperties = [node.GetVehicleProperty()],
                NodePosition = new NodePosition(){X=node.X,Y=node.Y},
                NodeDescription = node.Type.ToString()
            };
        }

        private VehicleTypeNodeProperty GetVehicleProperty()
        {
            List<VehicleTypeNodePropertyAction> actions = [];
            if (node.IsCharger())
            {
                actions.Add(node.GenerateChargeAction());
            }
            if (node.IsDropStation())
            {
                actions.Add(node.GenerateDropAction());
            }
            if (node.IsPickStation())
            {
                actions.Add(node.GeneratePickAction());
            }

            return new VehicleTypeNodeProperty()
            {
                Theta = node.Theta,
                Actions = actions.ToArray(),
                VehicleTypeId = "rikeb.series"
            };
        }

        private VehicleTypeNodePropertyAction GeneratePickAction()
        {
            return new VehicleTypeNodePropertyAction()
            {
                RequirementType = RequirementType.Optional,
                ActionDescription = "pick pallet",
                ActionType = "pick",
                BlockingType = BlockingType.Hard,
                ActionParameters = [new FluffyActionParameter {Key = "stationType", Value = "active conveyor" }, new FluffyActionParameter(){Key = "loadType", Value = "pallet" }]
            };
        }

        private VehicleTypeNodePropertyAction GenerateDropAction()
        {
            return new VehicleTypeNodePropertyAction()
            {
                RequirementType = RequirementType.Optional,
                ActionDescription = "drop pallet",
                ActionType = "drop",
                BlockingType = BlockingType.Hard,
                ActionParameters = [new FluffyActionParameter { Key = "stationType", Value = "active conveyor" }, new FluffyActionParameter() { Key = "loadType", Value = "pallet" }]
            };
        }

        private VehicleTypeNodePropertyAction GenerateChargeAction()
        {
            return new VehicleTypeNodePropertyAction()
            {
                RequirementType = RequirementType.Optional,
                ActionDescription = "start charging",
                ActionType = "startCharging",
                BlockingType = BlockingType.Hard,
            };
        }
    }
}
