using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs;
using JetBrains.Annotations;
using Vdma.Lif;
using LifNode = Vdma.Lif.Node;
using LifEdge = Vdma.Lif.Edge;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Extensions;

/// <summary>
/// Extension methods for LIF types.
/// </summary>
[PublicAPI]
public static class LifToGraphLayoutExtensions
{
    extension(LifLayoutCollection lifLayoutCollection)
    {
        /// <summary>
        /// Converts a <see cref="LifLayoutCollection"/> into a collection of <see cref="GraphLayout"/> domain models.
        /// </summary>
        /// <param name="storedAt">Timestamp applied to every resulting layout.</param>
        /// <param name="exportedAt">Timestamp when the LIF was exported.</param>
        /// <returns>One <see cref="GraphLayout"/> per LIF layout.</returns>
        public ICollection<GraphLayout> ToGraphLayouts(DateTime storedAt, DateTime exportedAt)
        {
            return lifLayoutCollection.Layouts
                .Select(layout => layout.ToGraphLayout(storedAt, exportedAt))
                .ToList();
        }
    }

    extension(Layout lifLayout)
    {
        /// <summary>
        /// Converts a single LIF <see cref="Layout"/> into a <see cref="GraphLayout"/> domain model.
        /// </summary>
        /// <param name="storedAt">Timestamp applied to the resulting layout.</param>
        /// <param name="exportedAt">Timestamp when the LIF was exported.</param>
        public GraphLayout ToGraphLayout(DateTime storedAt, DateTime exportedAt)
        {
            List<GraphLayoutNode> nodes = lifLayout.Nodes
                .Select(n =>
                {
                    float theta = (float)(lifLayout.Stations
                        .Where(s => s.InteractionNodeIds.Contains(n.NodeId))
                        .Select(s => s.StationPosition?.Theta)
                        .FirstOrDefault(t => t.HasValue) ?? 0);
                    return n.ToGraphLayoutNode(exportedAt, theta);
                })
                .ToList();

            List<GraphLayoutEdge> edges = lifLayout.Edges
                .Select(e => e.ToGraphLayoutEdge(exportedAt))
                .ToList();

            List<GraphLayoutStation> stations = lifLayout.Stations
                .Select(s => s.ToGraphLayoutStation())
                .ToList();

            return new GraphLayout
            {
                Id = Guid.NewGuid(),
                LayoutId = lifLayout.LayoutId,
                LayoutVersion = lifLayout.LayoutVersion,
                LayoutName = lifLayout.LayoutName,
                StoredAt = storedAt,
                Nodes = nodes,
                Edges = edges,
                Stations = stations
            };
        }
    }

    extension(LifNode lifNode)
    {
        /// <summary>
        /// Converts a LIF <see cref="LifNode"/> into a <see cref="GraphLayoutNode"/>.
        /// </summary>
        public GraphLayoutNode ToGraphLayoutNode(DateTime exportedTime, float theta = 0)
        {
            return new GraphLayoutNode
            {
                Id = lifNode.NodeId.ToGuid(),
                NodeId = lifNode.NodeId,
                NodeName = lifNode.NodeName,
                MapId = lifNode.MapId,
                PositionX = lifNode.NodePosition.X,
                PositionY = lifNode.NodePosition.Y,
                Theta = theta,
                Timestamp = exportedTime,
                VehicleTypeProperties = lifNode.VehicleTypeNodeProperties
                    .Select(vp => vp.ToNodeVehicleTypeProperty())
                    .ToList()
            };
        }
    }

    extension(VehicleTypeNodeProperty lifNodeProperty)
    {
        /// <summary>
        /// Converts a LIF <see cref="VehicleTypeNodeProperty"/> into a <see cref="NodeVehicleTypeProperty"/>.
        /// </summary>
        public NodeVehicleTypeProperty ToNodeVehicleTypeProperty()
        {
            return new NodeVehicleTypeProperty
            {
                VehicleTypeId = lifNodeProperty.VehicleTypeId,
                Theta = lifNodeProperty.Theta,
                Actions = (lifNodeProperty.Actions ?? [])
                    .Select(a => a.ToNodeVehicleTypeAction())
                    .ToList()
            };
        }
    }

    extension(VehicleTypeNodePropertyAction lifNodeAction)
    {
        /// <summary>
        /// Converts a LIF <see cref="VehicleTypeNodePropertyAction"/> into a <see cref="NodeVehicleTypeAction"/>.
        /// </summary>
        public NodeVehicleTypeAction ToNodeVehicleTypeAction()
        {
            return new NodeVehicleTypeAction
            {
                ActionType = lifNodeAction.ActionType,
                BlockingType = lifNodeAction.BlockingType.ToCommonBlockingType(),
                RequirementType = lifNodeAction.RequirementType.ToDomainRequirementType(),
                Parameters = lifNodeAction.ActionParameters.ToDomainParameters()
            };
        }
    }

    extension(LifEdge lifEdge)
    {
        /// <summary>
        /// Converts a LIF <see cref="LifEdge"/> into a <see cref="GraphLayoutEdge"/>.
        /// </summary>
        public GraphLayoutEdge ToGraphLayoutEdge(DateTime exportedAt)
        {
            return new GraphLayoutEdge
            {
                Id = lifEdge.EdgeId.ToGuid(),
                EdgeId = lifEdge.EdgeId,
                StartNodeId = lifEdge.StartNodeId,
                EndNodeId = lifEdge.EndNodeId,
                Timestamp = exportedAt,
                VehicleTypeProperties = lifEdge.VehicleTypeEdgeProperties
                    .Select(vp => vp.ToEdgeVehicleTypeProperty())
                    .ToList()
            };
        }
    }

    extension(VehicleTypeEdgeProperty lifEdgeProperty)
    {
        /// <summary>
        /// Converts a LIF <see cref="VehicleTypeEdgeProperty"/> into an <see cref="EdgeVehicleTypeProperty"/>.
        /// </summary>
        public EdgeVehicleTypeProperty ToEdgeVehicleTypeProperty()
        {
            return new EdgeVehicleTypeProperty
            {
                VehicleTypeId = lifEdgeProperty.VehicleTypeId,
                VehicleOrientation = lifEdgeProperty.VehicleOrientation,
                OrientationType = lifEdgeProperty.OrientationType.ToDomainOrientationType(),
                RotationAllowed = lifEdgeProperty.RotationAllowed,
                MaxSpeed = lifEdgeProperty.MaxSpeed,
                MaxRotationSpeed = lifEdgeProperty.MaxRotationSpeed,
                MaxHeight = lifEdgeProperty.MaxHeight,
                MinHeight = lifEdgeProperty.MinHeight,
                Trajectory = lifEdgeProperty.Trajectory.ToDomainTrajectory(),
                Actions = (lifEdgeProperty.Actions ?? [])
                    .Select(a => a.ToEdgeVehicleTypeAction())
                    .ToList()
            };
        }
    }

    extension(VehicleTypeEdgePropertyAction lifEdgeAction)
    {
        /// <summary>
        /// Converts a LIF <see cref="VehicleTypeEdgePropertyAction"/> into an <see cref="EdgeVehicleTypeAction"/>.
        /// </summary>
        public EdgeVehicleTypeAction ToEdgeVehicleTypeAction()
        {
            return new EdgeVehicleTypeAction
            {
                ActionType = lifEdgeAction.ActionType,
                BlockingType = lifEdgeAction.BlockingType.ToCommonBlockingType(),
                RequirementType = lifEdgeAction.RequirementType.ToDomainRequirementType(),
                Parameters = lifEdgeAction.ActionParameters.ToDomainParameters()
            };
        }
    }

    extension(Station lifStation)
    {
        /// <summary>
        /// Converts a LIF <see cref="Station"/> into a <see cref="GraphLayoutStation"/>.
        /// </summary>
        public GraphLayoutStation ToGraphLayoutStation()
        {
            return new GraphLayoutStation
            {
                StationId = lifStation.StationId,
                StationName = lifStation.StationName,
                StationDescription = lifStation.StationDescription,
                PositionX = lifStation.StationPosition?.X,
                PositionY = lifStation.StationPosition?.Y,
                Theta = lifStation.StationPosition?.Theta,
                InteractionNodeIds = lifStation.InteractionNodeIds.ToList()
            };
        }
    }
}