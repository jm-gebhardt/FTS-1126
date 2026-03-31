using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Graphs;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Extensions;

/// <summary>
/// Extensions for graph layout models.
/// </summary>
[PublicAPI]
public static class GraphLayoutExtensions
{
    extension(ICollection<GraphLayout> layouts)
    {
        /// <summary>
        /// Convert the stored graph layouts into a vehicle-type-specific <see cref="AgvGraph"/>.
        /// </summary>
        /// <param name="vehicleTypeId">The vehicle type to filter for.</param>
        /// <param name="agvCorrelationId">The correlation Id of the AGV to set on the graph.</param>
        /// <returns>The projected domain graph.</returns>
        public AgvGraph ToAgvGraph(string vehicleTypeId, Guid agvCorrelationId)
        {
            var nodes = new List<Node>();
            var edges = new List<Edge>();

            foreach (var layout in layouts)
            {
                foreach (var layoutNode in layout.Nodes)
                {
                    var node = layoutNode.ToNode(vehicleTypeId, agvCorrelationId, layout.LayoutId);
                    if (node != null)
                    {
                        nodes.Add(node);
                    }
                }

                foreach (var layoutEdge in layout.Edges)
                {
                    var edge = layoutEdge.ToEdge(vehicleTypeId, agvCorrelationId);
                    if (edge != null)
                    {
                        edges.Add(edge);
                    }
                }
            }

            edges.ApplyFallbackTrajectories(nodes);

            return new AgvGraph(agvCorrelationId, nodes, edges);
        }

        /// <summary>
        /// Get all distinct vehicle type Ids from the given layouts.
        /// </summary>
        public HashSet<string> GetVehicleTypeIds()
        {
            var vehicleTypeIds = new HashSet<string>();

            foreach (var layout in layouts)
            {
                foreach (var node in layout.Nodes)
                {
                    foreach (var prop in node.VehicleTypeProperties)
                    {
                        vehicleTypeIds.Add(prop.VehicleTypeId);
                    }
                }

                foreach (var edge in layout.Edges)
                {
                    foreach (var prop in edge.VehicleTypeProperties)
                    {
                        vehicleTypeIds.Add(prop.VehicleTypeId);
                    }
                }
            }

            return vehicleTypeIds;
        }
    }

    extension(GraphLayoutNode layoutNode)
    {
        /// <summary>
        /// Converts a <see cref="GraphLayoutNode"/> to a vehicle-type-specific <see cref="Node"/>.
        /// </summary>
        /// <param name="vehicleTypeId">The vehicle type to filter for.</param>
        /// <param name="agvCorrelationId">The AGV correlation Id.</param>
        /// <param name="layoutId">The layout Id used as fallback map Id.</param>
        /// <returns>The converted node, or null if the vehicle type has no properties.</returns>
        public Node? ToNode(string vehicleTypeId, Guid agvCorrelationId, string layoutId)
        {
            var vehicleProps = layoutNode.VehicleTypeProperties
                .SingleOrDefault(p => p.VehicleTypeId == vehicleTypeId);

            if (vehicleProps == null)
            {
                return null;
            }

            return new Node
            {
                Id = layoutNode.Id,
                NodeId = layoutNode.NodeName ?? layoutNode.NodeId,
                Position = new NodePosition(layoutNode.PositionX, layoutNode.PositionY),
                MapId = layoutNode.MapId ?? layoutId,
                Theta = layoutNode.Theta,
                AgvActions = vehicleProps.Actions.Select(a => new AgvNodeAction
                {
                    ActionType = a.ActionType,
                    AgvCorrelationId = agvCorrelationId,
                    BlockingType = a.BlockingType,
                    NodeId = layoutNode.Id,
                    Theta = vehicleProps.Theta,
                    RequirementType = a.RequirementType,
                    Parameters = a.Parameters.ToList()
                }).ToList()
            };
        }
    }

    extension(GraphLayoutEdge layoutEdge)
    {
        /// <summary>
        /// Converts a <see cref="GraphLayoutEdge"/> to a vehicle-type-specific <see cref="Edge"/>.
        /// </summary>
        /// <param name="vehicleTypeId">The vehicle type to filter for.</param>
        /// <param name="agvCorrelationId">The AGV correlation Id.</param>
        /// <returns>The converted edge, or null if the vehicle type has no properties.</returns>
        public Edge? ToEdge(string vehicleTypeId, Guid agvCorrelationId)
        {
            var vehicleProps = layoutEdge.VehicleTypeProperties
                .SingleOrDefault(p => p.VehicleTypeId == vehicleTypeId);

            if (vehicleProps == null)
            {
                return null;
            }

            return new Edge
            {
                Id = layoutEdge.Id,
                EdgeId = layoutEdge.EdgeId,
                StartNodeId = layoutEdge.StartNodeId,
                EndNodeId = layoutEdge.EndNodeId,
                AgvOrientation = vehicleProps.VehicleOrientation,
                OrientationType = vehicleProps.OrientationType,
                RotationAllowed = vehicleProps.RotationAllowed,
                MaxSpeed = vehicleProps.MaxSpeed,
                MaxRotationSpeed = vehicleProps.MaxRotationSpeed,
                MaxHeight = vehicleProps.MaxHeight,
                MinHeight = vehicleProps.MinHeight,
                AgvTrajectory = vehicleProps.Trajectory ?? new Trajectory(),
                AgvActions = vehicleProps.Actions.Select(a => new AgvEdgeAction
                {
                    ActionType = a.ActionType,
                    AgvCorrelationId = agvCorrelationId,
                    BlockingType = a.BlockingType,
                    EdgeId = layoutEdge.Id,
                    RequirementType = a.RequirementType,
                    Parameters = a.Parameters.ToList()
                }).ToList()
            };
        }
    }
}