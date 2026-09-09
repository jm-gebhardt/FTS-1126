using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Helper;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;
using JetBrains.Annotations;
using VdmaBlockingType = Vdma.Lif.BlockingType;
using VdmaControlPoint = Vdma.Lif.ControlPoint;
using VdmaEdge = Vdma.Lif.Edge;
using VdmaEdgeActionParameter = Vdma.Lif.PurpleActionParameter;
using VdmaLayout = Vdma.Lif.Layout;
using VdmaLifLayoutCollection = Vdma.Lif.LifLayoutCollection;
using VdmaLoadRestriction = Vdma.Lif.LoadRestriction;
using VdmaMetaInformation = Vdma.Lif.MetaInformation;
using VdmaNode = Vdma.Lif.Node;
using VdmaNodeActionParameter = Vdma.Lif.FluffyActionParameter;
using VdmaNodePosition = Vdma.Lif.NodePosition;
using VdmaRequirementType = Vdma.Lif.RequirementType;
using VdmaStation = Vdma.Lif.Station;
using VdmaStationPosition = Vdma.Lif.StationPosition;
using VdmaTrajectory = Vdma.Lif.Trajectory;
using VdmaVehicleTypeEdgeProperty = Vdma.Lif.VehicleTypeEdgeProperty;
using VdmaVehicleTypeEdgePropertyAction = Vdma.Lif.VehicleTypeEdgePropertyAction;
using VdmaVehicleTypeNodeProperty = Vdma.Lif.VehicleTypeNodeProperty;
using VdmaVehicleTypeNodePropertyAction = Vdma.Lif.VehicleTypeNodePropertyAction;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Extensions;

/// <summary>
/// Extension methods for converting <see cref="Vdma.Lif"/> types to their domain equivalents in
/// <see cref="Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif"/>.
/// </summary>
[PublicAPI]
public static class VdmaLifExtensions
{
    extension(VdmaLifLayoutCollection collection)
    {
        /// <summary>
        /// Converts a <see cref="VdmaLifLayoutCollection"/> to a domain <see cref="LifLayoutCollection"/>.
        /// </summary>
        public LifLayoutCollection ToLifLayoutCollection()
        {
            var metaInfo = collection.MetaInformation.ToMetaInformation();
            return new LifLayoutCollection(
                layouts: collection.Layouts.Select(l => l.ToLayout(metaInfo)).ToArray(),
                metaInformation: metaInfo
            );
        }
    }

    extension(VdmaMetaInformation metaInformation)
    {
        /// <summary>
        /// Converts a <see cref="VdmaMetaInformation"/> to a domain <see cref="MetaInformation"/>.
        /// </summary>
        public MetaInformation ToMetaInformation()
        {
            return new MetaInformation(
                creator: metaInformation.Creator,
                exportTimestamp: metaInformation.ExportTimestamp,
                lifVersion: metaInformation.LifVersion,
                projectIdentification: metaInformation.ProjectIdentification
            );
        }
    }

    extension(VdmaLayout layout)
    {
        /// <summary>
        /// Converts a <see cref="VdmaLayout"/> to a domain <see cref="Layout"/>.
        /// </summary>
        public Layout ToLayout(MetaInformation metaInformation)
        {
            var l = new Layout(
                id: InternalIdGenerator.GetLayoutId(metaInformation.Creator, layout.LayoutId),
                layoutId: layout.LayoutId,
                layoutVersion: layout.LayoutVersion,
                edges: layout.Edges.Select(e => e.ToEdge()).ToArray(),
                nodes: layout.Nodes.Select(n => n.ToNode()).ToArray(),
                stations: layout.Stations.Select(s => s.ToStation()).ToArray(),
                layoutDescription: layout.LayoutDescription,
                layoutLevelId: layout.LayoutLevelId,
                layoutName: layout.LayoutName
            );
            foreach (var station in l.Stations)
            {
                station.FleetManagerNodeIds = l.Nodes.Where(n => station.InteractionNodeIds.Contains(n.NodeId)).SelectMany(n => n.GetFleetManagerNodeIds()).Distinct().ToArray();
            }
            return l;
        }
    }

    extension(VdmaNode node)
    {
        /// <summary>
        /// Converts a <see cref="VdmaNode"/> to a domain <see cref="Node"/>.
        /// </summary>
        public Node ToNode()
        {
            return new Node(
                nodeId: node.NodeId,
                nodePosition: node.NodePosition.ToNodePosition(),
                vehicleTypeNodeProperties: node.VehicleTypeNodeProperties.Select(p => p.ToVehicleTypeNodeProperty()).ToArray(),
                mapId: node.MapId,
                nodeDescription: node.NodeDescription,
                nodeName: node.NodeName
            );
        }
    }

    extension(VdmaNodePosition position)
    {
        /// <summary>
        /// Converts a <see cref="VdmaNodePosition"/> to a domain <see cref="NodePosition"/>.
        /// </summary>
        public NodePosition ToNodePosition()
        {
            return new NodePosition(position.X, position.Y);
        }
    }

    extension(VdmaVehicleTypeNodeProperty property)
    {
        /// <summary>
        /// Converts a <see cref="VdmaVehicleTypeNodeProperty"/> to a domain <see cref="VehicleTypeNodeProperty"/>.
        /// </summary>
        public VehicleTypeNodeProperty ToVehicleTypeNodeProperty()
        {
            return new VehicleTypeNodeProperty(
                fleetManagerNodeId: Guid.NewGuid(),
                vehicleTypeId: property.VehicleTypeId,
                actions: property.Actions?.Select(a => a.ToVehicleTypeNodePropertyAction()).ToArray(),
                theta: property.Theta
            );
        }
    }

    extension(VdmaVehicleTypeNodePropertyAction action)
    {
        /// <summary>
        /// Converts a <see cref="VdmaVehicleTypeNodePropertyAction"/> to a domain <see cref="VehicleTypeNodePropertyAction"/>.
        /// </summary>
        public VehicleTypeNodePropertyAction ToVehicleTypeNodePropertyAction()
        {
            return new VehicleTypeNodePropertyAction(
                actionType: action.ActionType,
                blockingType: action.BlockingType.ToBlockingType(),
                actionDescription: action.ActionDescription,
                actionParameters: action.ActionParameters?.Select(p => p.ToNodeActionParameter()).ToArray(),
                requirementType: action.RequirementType?.ToRequirementType()
            );
        }
    }

    extension(VdmaNodeActionParameter parameter)
    {
        /// <summary>
        /// Converts a <see cref="VdmaNodeActionParameter"/> to a domain <see cref="NodeActionParameter"/>.
        /// </summary>
        public NodeActionParameter ToNodeActionParameter()
        {
            return new NodeActionParameter(parameter.Key, parameter.Value);
        }
    }

    extension(VdmaEdge edge)
    {
        /// <summary>
        /// Converts a <see cref="VdmaEdge"/> to a domain <see cref="Edge"/>.
        /// </summary>
        public Edge ToEdge()
        {
            return new Edge(
                edgeId: edge.EdgeId,
                endNodeId: edge.EndNodeId,
                startNodeId: edge.StartNodeId,
                vehicleTypeEdgeProperties: edge.VehicleTypeEdgeProperties.Select(p => p.ToVehicleTypeEdgeProperty()).ToArray()
            );
        }
    }

    extension(VdmaVehicleTypeEdgeProperty property)
    {
        /// <summary>
        /// Converts a <see cref="VdmaVehicleTypeEdgeProperty"/> to a domain <see cref="VehicleTypeEdgeProperty"/>.
        /// </summary>
        public VehicleTypeEdgeProperty ToVehicleTypeEdgeProperty()
        {
            return new VehicleTypeEdgeProperty(
                fleetManagerEdgeId: Guid.NewGuid(),
                vehicleTypeId: property.VehicleTypeId,
                actions: property.Actions?.Select(a => a.ToVehicleTypeEdgePropertyAction()).ToArray(),
                loadRestriction: property.LoadRestriction?.ToLoadRestriction(),
                maxHeight: property.MaxHeight,
                maxRotationSpeed: property.MaxRotationSpeed,
                maxSpeed: property.MaxSpeed,
                minHeight: property.MinHeight,
                orientationType: property.OrientationType,
                reentryAllowed: property.ReentryAllowed,
                rotationAllowed: property.RotationAllowed,
                rotationAtEndNodeAllowed: property.RotationAtEndNodeAllowed,
                rotationAtStartNodeAllowed: property.RotationAtStartNodeAllowed,
                trajectory: property.Trajectory?.ToTrajectory(),
                vehicleOrientation: property.VehicleOrientation
            );
        }
    }

    extension(VdmaVehicleTypeEdgePropertyAction action)
    {
        /// <summary>
        /// Converts a <see cref="VdmaVehicleTypeEdgePropertyAction"/> to a domain <see cref="VehicleTypeEdgePropertyAction"/>.
        /// </summary>
        public VehicleTypeEdgePropertyAction ToVehicleTypeEdgePropertyAction()
        {
            return new VehicleTypeEdgePropertyAction(
                actionType: action.ActionType,
                blockingType: action.BlockingType.ToBlockingType(),
                actionDescription: action.ActionDescription,
                actionParameters: action.ActionParameters?.Select(p => p.ToEdgeActionParameter()).ToArray(),
                requirementType: action.RequirementType?.ToRequirementType()
            );
        }
    }

    extension(VdmaEdgeActionParameter parameter)
    {
        /// <summary>
        /// Converts a <see cref="VdmaEdgeActionParameter"/> to a domain <see cref="EdgeActionParameter"/>.
        /// </summary>
        public EdgeActionParameter ToEdgeActionParameter()
        {
            return new EdgeActionParameter(parameter.Key, parameter.Value);
        }
    }

    extension(VdmaTrajectory trajectory)
    {
        /// <summary>
        /// Converts a <see cref="VdmaTrajectory"/> to a domain <see cref="Trajectory"/>.
        /// </summary>
        public Trajectory ToTrajectory()
        {
            return new Trajectory(
                controlPoints: trajectory.ControlPoints.Select(cp => cp.ToControlPoint()).ToArray(),
                knotVector: trajectory.KnotVector,
                degree: trajectory.Degree
            );
        }
    }

    extension(VdmaControlPoint controlPoint)
    {
        /// <summary>
        /// Converts a <see cref="VdmaControlPoint"/> to a domain <see cref="ControlPoint"/>.
        /// </summary>
        public ControlPoint ToControlPoint()
        {
            return new ControlPoint(controlPoint.X, controlPoint.Y, controlPoint.Weight);
        }
    }

    extension(VdmaStation station)
    {
        /// <summary>
        /// Converts a <see cref="VdmaStation"/> to a domain <see cref="Station"/>.
        /// </summary>
        public Station ToStation()
        {
            return new Station(
                fleetManagerId: Guid.NewGuid(),
                fleetManagerNodeIds: [],
                stationId: station.StationId,
                interactionNodeIds: station.InteractionNodeIds.ToArray(),
                stationDescription: station.StationDescription,
                stationHeight: station.StationHeight,
                stationName: station.StationName,
                stationPosition: station.StationPosition?.ToStationPosition()
            );
        }
    }

    extension(VdmaStationPosition position)
    {
        /// <summary>
        /// Converts a <see cref="VdmaStationPosition"/> to a domain <see cref="StationPosition"/>.
        /// </summary>
        public StationPosition ToStationPosition()
        {
            return new StationPosition(position.X, position.Y, position.Theta);
        }
    }

    extension(VdmaLoadRestriction loadRestriction)
    {
        /// <summary>
        /// Converts a <see cref="VdmaLoadRestriction"/> to a domain <see cref="LoadRestriction"/>.
        /// </summary>
        public LoadRestriction ToLoadRestriction()
        {
            return new LoadRestriction(
                loaded: loadRestriction.Loaded,
                unloaded: loadRestriction.Unloaded,
                loadSetNames: loadRestriction.LoadSetNames
            );
        }
    }

    private static BlockingType ToBlockingType(this VdmaBlockingType blockingType) => blockingType switch
    {
        VdmaBlockingType.Hard => BlockingType.HARD,
        VdmaBlockingType.Soft => BlockingType.SOFT,
        _ => BlockingType.NONE
    };

    private static RequirementType ToRequirementType(this VdmaRequirementType requirementType) => requirementType switch
    {
        VdmaRequirementType.Required => RequirementType.Required,
        VdmaRequirementType.Optional => RequirementType.Optional,
        VdmaRequirementType.Conditional => RequirementType.Conditional,
        _ => RequirementType.Optional
    };

    extension(double? value)
    {
        /// <summary>
        /// Returns true, if theta is out of range as defined by vda 5050.
        /// </summary>
        public bool AngleOutOfRange()
        {
            return value != null && (value < -Math.PI || value > Math.PI);
        }

        /// <summary>
        /// Returns true, if the given value is lower than zero.
        /// </summary>
        public bool IsNegative()
        {
            return value is < 0;
        }
    }
}
