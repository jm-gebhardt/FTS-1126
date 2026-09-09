using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Gebhardt.Acc.Common.Domain.Models.NewGraph;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Helper;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;
using CommonEdge = Gebhardt.Acc.Common.Domain.Models.NewGraph.Edge;
using CommonNode = Gebhardt.Acc.Common.Domain.Models.NewGraph.Node;
using CommonStation = Gebhardt.Acc.Common.Domain.Models.NewGraph.Station;
using OrientationType = Gebhardt.Acc.Common.Domain.Models.NewGraph.OrientationType;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.LIF.LifToGraphConversion;

/// <summary>
/// Converts LIF layouts into a <see cref="Graph"/>.
/// </summary>
public static class LifToGraphConverter
{
    /// <summary>
    /// Generates one graph out of the given layouts.
    /// </summary>
    public static Graph ToGraph(LifLayoutCollection layoutCollection, IReadOnlyDictionary<string, Guid> agvTypeIdentifiers, ILogger logger)
    {
        var container = new GraphContainer(agvTypeIdentifiers)
                        {
                            LifId = layoutCollection.MetaInformation.Creator
                        };

        layoutCollection.Layouts.ForEach(layout => AddNodes(layout, container));
        layoutCollection.Layouts.ForEach(layout => AddEdges(layout, container));
        layoutCollection.Layouts.ForEach(layout => AddStations(layout, container));

        AddRoutingNodes(container, logger);
        AddRoutingEdges(container, logger);

        var graph = new Graph(container.GetNodes(), container.GetEdges(), container.GetStations());

        return graph;
    }

    private static void AddNodes(Layout layout, GraphContainer container)
    {
        foreach (var node in layout.Nodes)
        {
            var nodeId = InternalIdGenerator.GetNodeId(container.LifId, node.NodeId, out string name);

            container.Add(node, new CommonNode(nodeId, name, node.NodeId, node.NodePosition.X, node.NodePosition.Y, layout.LayoutLevelId));
        }
    }

    private static void AddEdges(Layout layout, GraphContainer container)
    {
        foreach (var edge in layout.Edges)
        {
            var edgeId = InternalIdGenerator.GetEdgeId(container.LifId, edge.EdgeId, out string name);
            var startNodeId = InternalIdGenerator.GetNodeId(container.LifId, edge.StartNodeId, out _);
            var endNodeId = InternalIdGenerator.GetNodeId(container.LifId, edge.EndNodeId, out _);

            container.Add(edge, new CommonEdge(edgeId, name, edge.EdgeId, startNodeId, endNodeId));
        }
    }

    private static void AddStations(Layout layout, GraphContainer container)
    {
        foreach (var station in layout.Stations)
        {
            var internalId = InternalIdGenerator.GetStationId(container.LifId, station.StationId, out string name);
            List<Guid> internalInteractionNodeIds = station.InteractionNodeIds.Select(id => InternalIdGenerator.GetNodeId(container.LifId, id, out _)).ToList();

            if (station.StationPosition is not { } stationPosition)
            {
                throw new ArgumentException($"Station {layout.LayoutId} - {station.StationName} has no position");
            }

            var commonStation = new CommonStation(internalId, name, station.StationId, station.StationHeight, stationPosition.X, stationPosition.Y, stationPosition.Theta)
            {
                InteractionNodeIds = internalInteractionNodeIds
            };

            container.Add(commonStation);
        }
    }

    private static void AddRoutingNodes(GraphContainer container, ILogger logger)
    {
        foreach (var nodeHelper in container.GetNodeHelpers())
        {
            var nodeId = InternalIdGenerator.GetNodeId(container.LifId, nodeHelper.Node.NodeId, out _);

            foreach (string vehicleTypeId in nodeHelper.VehicleTypeIds)
            {
                if (!container.TryGetAgvTypeId(vehicleTypeId, out var agvTypeId))
                {
                    continue;
                }

                foreach (var loadStateId in GetLoadStateIds(vehicleTypeId))
                {
                    List<double> thetas = nodeHelper.GetThetas(vehicleTypeId);

                    if (thetas.Count == 0)
                    {
                        logger.LogWarning("No thetas '{VehicleTypeId}' at node '{NodeId}'", vehicleTypeId, nodeHelper.Node.NodeId);
                    }

                    foreach (double theta in thetas)
                    {
                        container.Add(nodeId, new RoutingNode(Guid.NewGuid(), agvTypeId, loadStateId, theta));
                    }
                }
            }
        }
    }

    private static void AddRoutingEdges(GraphContainer container, ILogger logger)
    {
        foreach (var edgeHelper in container.GetEdgeHelpers())
        {
            var lifEdge = edgeHelper.Edge;

            var edgeId = InternalIdGenerator.GetEdgeId(container.LifId, lifEdge.EdgeId, out _);
            var startNodeId = InternalIdGenerator.GetNodeId(container.LifId, lifEdge.StartNodeId, out _);
            var endNodeId = InternalIdGenerator.GetNodeId(container.LifId, lifEdge.EndNodeId, out _);

            Trace.Assert(startNodeId != edgeId);

            foreach (var edgePropertyHelper in edgeHelper.EdgePropertyHelpers)
            {
                AddRoutingEdgesForEdgeProperty(container, lifEdge, edgeId, startNodeId, endNodeId, edgePropertyHelper, logger);
            }
        }
    }

    private static void AddRoutingEdgesForEdgeProperty(GraphContainer container, Lif.Edge lifEdge, Guid edgeId, Guid startNodeId, Guid endNodeId, VehicleTypeEdgePropertyHelper edgePropertyHelper, ILogger logger)
    {
        var edgeProperty = edgePropertyHelper.EdgeProperty;

        if (!container.TryGetAgvTypeId(edgeProperty.VehicleTypeId, out var agvTypeId))
        {
            return;
        }

        var startNodeHelper = container.GetNodeHelper(startNodeId);
        var endNodeHelper = container.GetNodeHelper(endNodeId);

        List<Guid> loadStateIds = GetLoadStateIds(edgeProperty.VehicleTypeId, edgeProperty.LoadRestriction);

        foreach (var loadStateId in loadStateIds)
        {
            IReadOnlyCollection<RoutingNode> startRoutingNodes = container.GetRoutingNodes(startNodeId, agvTypeId, loadStateId);
            IReadOnlyCollection<RoutingNode> endRoutingNodes = container.GetRoutingNodes(endNodeId, agvTypeId, loadStateId);

            foreach (var startRoutingNode in startRoutingNodes)
            {
                foreach (var endRoutingNode in endRoutingNodes)
                {
                    if (startNodeHelper.NodePositionEquals(endNodeHelper)) // pure Rotation no trajectory
                    {
                        AddPureRotationRoutingEdge(container, edgeId, agvTypeId, edgePropertyHelper, startRoutingNode, endRoutingNode);
                    }
                    else // trajectory with optional rotation at start and end
                    {
                        AddTrajectoryRoutingEdge(container, lifEdge, edgeId, agvTypeId, edgePropertyHelper, startNodeHelper, endNodeHelper, startRoutingNode, endRoutingNode, logger);
                    }
                }
            }
        }
    }

    private static void AddPureRotationRoutingEdge(GraphContainer container,
        Guid edgeId,
        Guid agvTypeId,
        VehicleTypeEdgePropertyHelper edgePropertyHelper,
        RoutingNode startRoutingNode,
        RoutingNode endRoutingNode)
    {
        var edgeProperty = edgePropertyHelper.EdgeProperty;

        Trace.Assert(edgePropertyHelper.Trajectory is null);
        Trace.Assert(edgeProperty.VehicleOrientation is null);
        Trace.Assert(edgeProperty.RotationAllowed is true);

        double rotationAtStart = endRoutingNode.Theta - startRoutingNode.Theta; //todo: consider CCW/CW, shortest angle

        container.Add(edgeId, new RoutingEdge(Guid.NewGuid(),
            agvTypeId,
            startRoutingNode.Id,
            endRoutingNode.Id,
            0,
            null,
            OrientationType.Tangential,
            rotationAtStart,
            null,
            true,
            null,
            edgeProperty.MaxRotationSpeed,
            null));
    }

    private static void AddTrajectoryRoutingEdge(GraphContainer container,
        Lif.Edge lifEdge,
        Guid edgeId,
        Guid agvTypeId,
        VehicleTypeEdgePropertyHelper edgePropertyHelper,
        NodeHelper startNodeHelper,
        NodeHelper endNodeHelper,
        RoutingNode startRoutingNode,
        RoutingNode endRoutingNode,
        ILogger logger)
    {
        var edgeProperty = edgePropertyHelper.EdgeProperty;
        var trajectory = edgePropertyHelper.Trajectory;

        Trace.Assert(trajectory is not null);
        Trace.Assert(edgeProperty.VehicleOrientation is not null);
        Trace.Assert(edgePropertyHelper.EdgeStartTheta is not null);
        Trace.Assert(edgePropertyHelper.EdgeEndTheta is not null);

        double length = trajectory.CalculateLength();
        bool rotationAllowed = edgeProperty.RotationAllowed ?? false;

        double rotationAtStart = edgePropertyHelper.EdgeStartTheta.Value - startRoutingNode.Theta; //todo: consider CCW/CW, shortest angle

        if (!rotationAtStart.RotationEquals(0) && startNodeHelper.GetRotationAtNodeAllowed(edgeProperty.VehicleTypeId) == RotationAtNodeAllowed.None)
        {
            logger.LogWarning("Rotation at start of Edge '{EdgeId}' not zero but rotation at start node is not allowed for vehicle type '{VehicleTypeId}'", lifEdge.EdgeId, edgeProperty.VehicleTypeId);
            return;
        }

        double rotationAtEnd = edgePropertyHelper.EdgeEndTheta.Value - endRoutingNode.Theta; //todo: consider CCW/CW, shortest angle

        if (!rotationAtEnd.RotationEquals(0) && endNodeHelper.GetRotationAtNodeAllowed(edgeProperty.VehicleTypeId) == RotationAtNodeAllowed.None)
        {
            logger.LogWarning("Rotation at end of Edge '{EdgeId}' not zero but rotation at end node is not allowed for vehicle type '{VehicleTypeId}'", lifEdge.EdgeId, edgeProperty.VehicleTypeId);
            return;
        }

        container.Add(edgeId, new RoutingEdge(Guid.NewGuid(),
                                                agvTypeId,
                                                startRoutingNode.Id,
                                                endRoutingNode.Id,
                                                length,
                                                edgeProperty.VehicleOrientation,
                                                GetOrientationType(edgeProperty.OrientationType),
                                                rotationAtStart,
                                                rotationAtEnd,
                                                rotationAllowed,
                                                edgeProperty.MaxSpeed,
                                                edgeProperty.MaxRotationSpeed,
                                                trajectory));
    }

    /// <summary>
    /// Gets the load state ids based on the given load restriction. Simplified temporary logic with hardcoded load state ids which are matching in TransportOrder.
    /// </summary>
    private static List<Guid> GetLoadStateIds(string agvTypeIdentifier, LoadRestriction? loadRestriction)
    {
        var loadStateIds = new List<Guid>();

        if (loadRestriction is null || loadRestriction.Loaded)
        {
            loadStateIds.Add($"{agvTypeIdentifier}_loaded".ToGuid());
        }

        if (loadRestriction is null || loadRestriction.Unloaded)
        {
            loadStateIds.Add($"{agvTypeIdentifier}_unloaded".ToGuid());
        }

        return loadStateIds;
    }

    /// <summary>
    /// Gets all load state ids. Simplified temporary logic with hardcoded load state ids which are matching in TransportOrder.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private static List<Guid> GetLoadStateIds(string agvTypeIdentifier)
    {
        return [$"{agvTypeIdentifier}_loaded".ToGuid(), $"{agvTypeIdentifier}_unloaded".ToGuid()];
    }

    private static OrientationType GetOrientationType(string? orientationTypeString)
    {
        if (orientationTypeString == null || string.Equals(orientationTypeString, "TANGENTIAL", StringComparison.OrdinalIgnoreCase))
        {
            return OrientationType.Tangential;
        }

        if (string.Equals(orientationTypeString, "GLOBAL", StringComparison.OrdinalIgnoreCase))
        {
            return OrientationType.Global;
        }

        throw new ArgumentException($"Invalid orientation type: {orientationTypeString}");
    }
}