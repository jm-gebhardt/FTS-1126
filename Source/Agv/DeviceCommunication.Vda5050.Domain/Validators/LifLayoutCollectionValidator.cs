using FluentValidation;
using FluentValidation.Results;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Extensions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;
using JetBrains.Annotations;
using Edge = Vdma.Lif.Edge;
using Layout = Vdma.Lif.Layout;
using LifLayoutCollection = Vdma.Lif.LifLayoutCollection;
using Node = Vdma.Lif.Node;
using VehicleTypeEdgeProperty = Vdma.Lif.VehicleTypeEdgeProperty;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Validators;

/// <summary>
/// Validates a <see cref="Vdma.Lif.LifLayoutCollection"/>.
/// </summary>
[PublicAPI]
public class LifLayoutCollectionValidator : AbstractValidator<LifLayoutCollection>
{
    private const double PositionAccuracy = 0.05;

    /// <inheritdoc cref="LifLayoutCollectionValidator"/>
    public LifLayoutCollectionValidator()
    {
        RuleFor(x => x.MetaInformation.LifVersion)
            .Must(version => version.Split(".").First() == Vda5050Specifications.LifMajorVersion)
            .WithMessage("Unsupported lif version")
            .WithState(lc => new LifValidationResult(ResourceType.MetaInformation, nameof(lc.MetaInformation.LifVersion), Models.Lif.Severity.Err, $"LIF version {lc.MetaInformation.LifVersion.Split(".").First()} not supported"));

        RuleFor(x => x.Layouts)
            .NotEmpty()
            .WithMessage("LIF layout collection must contain at least one layout.")
            .WithState(_ => new LifValidationResult(ResourceType.General, null, Models.Lif.Severity.Err, "LIF layout collection must contain at least one layout"));

        RuleFor(x => x.Layouts).Custom(ValidateLayoutIds);
        RuleFor(x => x.Layouts).Custom(ValidateElementIds);

        RuleForEach(x => x.Layouts).ChildRules(layout =>
        {
            layout.RuleFor(l => l.Nodes)
                .NotEmpty()
                .WithMessage("Each layout must contain at least one node")
                .WithState(lc => new LifValidationResult(ResourceType.Layout, lc.LayoutId, Models.Lif.Severity.Err, $"Layout {lc.LayoutId} must contain at least one node"));

            layout.RuleFor(l => l.Edges)
                .NotEmpty()
                .WithMessage("Each layout must contain at least one edge")
                .WithState(lc => new LifValidationResult(ResourceType.Layout, lc.LayoutId, Models.Lif.Severity.Err, $"Layout {lc.LayoutId} must contain at least one edge"));

            layout.RuleFor(l => l).Custom((l, context) =>
            {
                var nodeIds = l.Nodes.Select(n => n.NodeId).ToHashSet();

                ValidateEdges(l, nodeIds, context);
                ValidateStations(l, context, nodeIds);
                ValidateNodes(l, context);
            });
        });
    }

    private static void ValidateLayoutIds(Layout[] l, ValidationContext<LifLayoutCollection> context)
    {
        var duplicateLayouts = l.GroupBy(g => g.LayoutId).Where(g => g.Count() > 1).SelectMany(g => g).ToList();
        foreach (var duplicateLayout in duplicateLayouts.DistinctBy(d => d.LayoutId))
        {
            string message = $"LayoutId '{duplicateLayout.LayoutId}' duplicated.";
            var failure = new ValidationFailure { ErrorMessage = message, CustomState = new LifValidationResult(ResourceType.Layout, duplicateLayout.LayoutId, Models.Lif.Severity.Err, message) };
            context.AddFailure(failure);
        }
    }

    private static void ValidateEdges(Layout l, HashSet<string> nodeIds, ValidationContext<Layout> context)
    {
        foreach (var edge in l.Edges)
        {
            if (!nodeIds.Contains(edge.StartNodeId))
            {
                string message = $"Edge '{edge.EdgeId}' references StartNodeId '{edge.StartNodeId}' which does not exist in layout '{l.LayoutId}'";
                var failure = new ValidationFailure { ErrorMessage = message, CustomState = new LifValidationResult(ResourceType.Edge, edge.EdgeId, Models.Lif.Severity.Err, message) };
                context.AddFailure(failure);
            }

            if (!nodeIds.Contains(edge.EndNodeId))
            {
                string message = $"Edge '{edge.EdgeId}' references EndNodeId '{edge.EndNodeId}' which does not exist in layout '{l.LayoutId}'";
                var failure = new ValidationFailure { ErrorMessage = message, CustomState = new LifValidationResult(ResourceType.Edge, edge.EdgeId, Models.Lif.Severity.Err, message) };
                context.AddFailure(failure);
            }

            if (!edge.VehicleTypeEdgeProperties.Any())
            {
                string message = $"Edge '{edge.EdgeId}' in layout '{l.LayoutId}' does not contain vehicle type edge properties";
                var failure = new ValidationFailure { ErrorMessage = message, CustomState = new LifValidationResult(ResourceType.Edge, edge.EdgeId, Models.Lif.Severity.Err, message) };
                context.AddFailure(failure);
            }
            var startNode = l.Nodes.FirstOrDefault(n => n.NodeId == edge.StartNodeId);
            var endNode = l.Nodes.FirstOrDefault(n => n.NodeId == edge.EndNodeId);
            var previousEdges = l.Edges.Where(e => string.Equals(e.EndNodeId, edge.StartNodeId, StringComparison.InvariantCultureIgnoreCase)).ToArray();
            foreach (var property in edge.VehicleTypeEdgeProperties)
            {
                ValidateTrajectory(property.Trajectory, edge, property, l, startNode, endNode, context);
                ValidateValueRanges(l, context, property, edge);
                ValidateOrientation(l, context, property, edge, previousEdges);
            }
        }
    }

    private static void ValidateOrientation(Layout l, ValidationContext<Layout> context, VehicleTypeEdgeProperty property, Edge edge, Edge[] previousEdges)
    {
        if (string.Equals(property.OrientationType, "GLOBAL", StringComparison.InvariantCultureIgnoreCase) || property.VehicleOrientation == null || property.RotationAllowed == null)
        {
            return;
        }
        bool rotationAllowedAtStart = property.RotationAtStartNodeAllowed == null || !string.Equals(property.RotationAtStartNodeAllowed, "NONE", StringComparison.InvariantCultureIgnoreCase);
        foreach (var previousEdge in previousEdges)
        {
            var previous = previousEdge.VehicleTypeEdgeProperties.SingleOrDefault(p => string.Equals(property.VehicleTypeId, p.VehicleTypeId, StringComparison.InvariantCultureIgnoreCase));
            if (previous == null)
            {
                continue;
            }
            if (string.Equals(previous.OrientationType, "GLOBAL", StringComparison.InvariantCultureIgnoreCase))
            {
                continue;
            }
            if (previous.VehicleOrientation == null)
            {
                continue;
            }
            bool orientationDiffers = Math.Abs(property.VehicleOrientation.Value - previous.VehicleOrientation.Value) > float.Epsilon;
            if (orientationDiffers && !rotationAllowedAtStart && !property.RotationAllowed.Value)
            {
                string message = $"Edge '{edge.EdgeId}' in layout '{l.LayoutId}' cannot be reached by vehicle type '{property.VehicleTypeId}' from edge {previousEdge.EdgeId} without violating its required vehicle orientation constraints";
                var failure = new ValidationFailure { ErrorMessage = message, CustomState = new LifValidationResult(ResourceType.Edge, edge.EdgeId, Models.Lif.Severity.Warn, message) };
                context.AddFailure(failure);
            }
        }
    }

    private static void ValidateValueRanges(Layout l, ValidationContext<Layout> context, VehicleTypeEdgeProperty property, Edge edge)
    {
        if (property.MaxHeight.IsNegative())
        {
            string message = $"Property {nameof(property.MaxHeight)} for vehicle type {property.VehicleTypeId} on edge {edge.EdgeId} in layout '{l.LayoutId}' must be >=0";
            var failure = new ValidationFailure { ErrorMessage = message, CustomState = new LifValidationResult(ResourceType.Edge, edge.EdgeId, Models.Lif.Severity.Err, message) };
            context.AddFailure(failure);
        }
        if (property.MinHeight.IsNegative())
        {
            string message = $"Property {nameof(property.MinHeight)} for vehicle type {property.VehicleTypeId} on edge {edge.EdgeId} in layout '{l.LayoutId}' must be >=0";
            var failure = new ValidationFailure { ErrorMessage = message, CustomState = new LifValidationResult(ResourceType.Edge, edge.EdgeId, Models.Lif.Severity.Err, message) };
            context.AddFailure(failure);
        }
        if (property.MaxSpeed.IsNegative())
        {
            string message = $"Property {nameof(property.MaxSpeed)} for vehicle type {property.VehicleTypeId} on edge {edge.EdgeId} in layout '{l.LayoutId}' must be >=0.";
            var failure = new ValidationFailure { ErrorMessage = message, CustomState = new LifValidationResult(ResourceType.Edge, edge.EdgeId, Models.Lif.Severity.Err, message) };
            context.AddFailure(failure);
        }
        if (property.MaxRotationSpeed.IsNegative())
        {
            string message = $"Property {nameof(property.MaxRotationSpeed)} for vehicle type {property.VehicleTypeId} on edge {edge.EdgeId} in layout '{l.LayoutId}' must be >=0";
            var failure = new ValidationFailure { ErrorMessage = message, CustomState = new LifValidationResult(ResourceType.Edge, edge.EdgeId, Models.Lif.Severity.Err, message) };
            context.AddFailure(failure);
        }
    }

    private static void ValidateNodes(Layout l, ValidationContext<Layout> context)
    {
        var startNodeIds = l.Edges.Select(e => e.StartNodeId).ToHashSet();
        var endNodeIds = l.Edges.Select(e => e.EndNodeId).ToHashSet();
        foreach (var node in l.Nodes)
        {
            if (!startNodeIds.Contains(node.NodeId) && !endNodeIds.Contains(node.NodeId))
            {
                string message = $"Node '{node.NodeId}' not connected to any edge in layout '{l.LayoutId}'";
                var failure = new ValidationFailure { ErrorMessage = message, CustomState = new LifValidationResult(ResourceType.Node, node.NodeId, Models.Lif.Severity.Warn, message) };
                context.AddFailure(failure);
            }
            if (!startNodeIds.Contains(node.NodeId))
            {
                string message = $"Dead node: '{node.NodeId}' not connected to any outgoing edge in layout '{l.LayoutId}'";
                var failure = new ValidationFailure { ErrorMessage = message, CustomState = new LifValidationResult(ResourceType.Node, node.NodeId, Models.Lif.Severity.Err, message) };
                context.AddFailure(failure);
            }
            if (!endNodeIds.Contains(node.NodeId))
            {
                string message = $"Unreachable node: '{node.NodeId}' not connected to any incoming edge in layout '{l.LayoutId}'";
                var failure = new ValidationFailure { ErrorMessage = message, CustomState = new LifValidationResult(ResourceType.Node, node.NodeId, Models.Lif.Severity.Err, message) };
                context.AddFailure(failure);
            }
            if (!node.VehicleTypeNodeProperties.Any())
            {
                string message = $"Node '{node.NodeId}' in layout '{l.LayoutId}' does not contain vehicle type node properties";
                var failure = new ValidationFailure { ErrorMessage = message, CustomState = new LifValidationResult(ResourceType.Node, node.NodeId, Models.Lif.Severity.Err, message) };
                context.AddFailure(failure);
            }
            foreach (var property in node.VehicleTypeNodeProperties)
            {
                if (property.Theta.AngleOutOfRange())
                {
                    string message = $"Theta {property.Theta} for vehicle type {property.VehicleTypeId} of node '{node.NodeId}' in layout '{l.LayoutId}' is out of range [-PI, PI]";
                    var failure = new ValidationFailure { ErrorMessage = message, CustomState = new LifValidationResult(ResourceType.Node, node.NodeId, Models.Lif.Severity.Warn, message) };
                    context.AddFailure(failure);
                }
            }
        }
    }

    private static void ValidateStations(Layout l, ValidationContext<Layout> context, HashSet<string> nodeIds)
    {
        foreach (var station in l.Stations)
        {
            if (!station.InteractionNodeIds.Any())
            {
                string message = $"Station '{station.StationId}' does not define interaction nodes'";
                var failure = new ValidationFailure { ErrorMessage = message, CustomState = new LifValidationResult(ResourceType.Station, station.StationId, Models.Lif.Severity.Err, message) };
                context.AddFailure(failure);
            }
            foreach (string node in station.InteractionNodeIds.Except(nodeIds))
            {
                string message = $"Station '{station.StationId}' references InteractionNodeId '{node}' which does not exist in layout '{l.LayoutId}'";
                var failure = new ValidationFailure { ErrorMessage = message, CustomState = new LifValidationResult(ResourceType.Station, station.StationId, Models.Lif.Severity.Err, message) };
                context.AddFailure(failure);
            }
            if (station.StationPosition?.Theta.AngleOutOfRange() ?? false)
            {
                string message = $"Theta {station.StationPosition?.Theta} of station '{station.StationId}' in layout '{l.LayoutId}' is out of range [-PI, PI]";
                var failure = new ValidationFailure { ErrorMessage = message, CustomState = new LifValidationResult(ResourceType.Station, station.StationId, Models.Lif.Severity.Warn, message) };
                context.AddFailure(failure);
            }
        }
    }

    private static void ValidateTrajectory(Vdma.Lif.Trajectory? trajectory, Edge e, VehicleTypeEdgeProperty p, Layout l, Node? startNode, Node? endNode, ValidationContext<Layout> context)
    {
        if (trajectory is null)
        {
            // Rotation routing edges don't have a trajectory
            return;
        }

        long degree = trajectory.Degree ?? 1;
        int cpCount = trajectory.ControlPoints.Length;
        if (cpCount < 2)
        {
            string message = $"Trajectory for vehicle type {p.VehicleTypeId} on edge {e.EdgeId} in layout '{l.LayoutId}' contains less than 2 control points";
            var failure = new ValidationFailure { ErrorMessage = message, CustomState = new LifValidationResult(ResourceType.Edge, e.EdgeId, Models.Lif.Severity.Err, message) };
            context.AddFailure(failure);
        }
        if (cpCount != degree + 1)
        {
            string message = $"Trajectory for vehicle type {p.VehicleTypeId} on edge {e.EdgeId} in layout '{l.LayoutId}': number of control points {cpCount} inconsistent with degree {trajectory.Degree}";
            var failure = new ValidationFailure { ErrorMessage = message, CustomState = new LifValidationResult(ResourceType.Edge, e.EdgeId, Models.Lif.Severity.Err, message) };
            context.AddFailure(failure);
        }
        int knVCount = trajectory.KnotVector.Length;
        if (knVCount != cpCount + degree + 1)
        {
            string message = $"Trajectory for vehicle type {p.VehicleTypeId} on edge {e.EdgeId} in layout '{l.LayoutId}': length of knot vector {knVCount} inconsistent with number of control points {cpCount} and degree {trajectory.Degree}";
            var failure = new ValidationFailure { ErrorMessage = message, CustomState = new LifValidationResult(ResourceType.Edge, e.EdgeId, Models.Lif.Severity.Err, message) };
            context.AddFailure(failure);
        }
        if (trajectory.KnotVector.Any(k => k is < 0 or > 1))
        {
            string message = $"Trajectory for vehicle type {p.VehicleTypeId} on edge {e.EdgeId} in layout '{l.LayoutId}': knot vector values need to be in range [0,1]";
            var failure = new ValidationFailure { ErrorMessage = message, CustomState = new LifValidationResult(ResourceType.Edge, e.EdgeId, Models.Lif.Severity.Err, message) };
            context.AddFailure(failure);
        }
        if (degree < 1)
        {
            string message = $"Trajectory for vehicle type {p.VehicleTypeId} on edge {e.EdgeId} in layout '{l.LayoutId}': degree needs to be in range [1, maxInteger]";
            var failure = new ValidationFailure { ErrorMessage = message, CustomState = new LifValidationResult(ResourceType.Edge, e.EdgeId, Models.Lif.Severity.Err, message) };
            context.AddFailure(failure);
        }
        if (startNode != null)
        {
            var firstCp = trajectory.ControlPoints.First();
            if (Math.Abs(firstCp.X - startNode.NodePosition.X) > PositionAccuracy || Math.Abs(firstCp.Y - startNode.NodePosition.Y) > PositionAccuracy)
            {
                string message = $"Trajectory for vehicle type {p.VehicleTypeId} on edge {e.EdgeId} in layout '{l.LayoutId}': first control point ({firstCp.X}, {firstCp.Y}) does not match edge start node ({startNode.NodePosition.X},{startNode.NodePosition.Y})";
                var failure = new ValidationFailure { ErrorMessage = message, CustomState = new LifValidationResult(ResourceType.Edge, e.EdgeId, Models.Lif.Severity.Err, message) };
                context.AddFailure(failure);
            }
        }
        if (endNode != null)
        {
            var lastCp = trajectory.ControlPoints.Last();
            if (Math.Abs(lastCp.X - endNode.NodePosition.X) > PositionAccuracy || Math.Abs(lastCp.Y - endNode.NodePosition.Y) > PositionAccuracy)
            {
                string message = $"Trajectory for vehicle type {p.VehicleTypeId} on edge {e.EdgeId} in layout '{l.LayoutId}': last control point ({lastCp.X}, {lastCp.Y}) does not match edge end node ({endNode.NodePosition.X},{endNode.NodePosition.Y})";
                var failure = new ValidationFailure { ErrorMessage = message, CustomState = new LifValidationResult(ResourceType.Edge, e.EdgeId, Models.Lif.Severity.Err, message) };
                context.AddFailure(failure);
            }
        }
    }

    private static void ValidateElementIds(Layout[] layouts, ValidationContext<LifLayoutCollection> context)
    {
        var stations = layouts.SelectMany(l => l.Stations.Select(s => (Layout: l, Station: s)));
        foreach (var duplicateGroup in stations.GroupBy(x => x.Station.StationId).Where(g => g.Count() > 1))
        {
            string message = $"StationId '{duplicateGroup.Key}' duplicated";
            var failure = new ValidationFailure { ErrorMessage = message, CustomState = new LifValidationResult(ResourceType.Station, duplicateGroup.Key, Models.Lif.Severity.Err, message) };
            context.AddFailure(failure);
        }

        var edges = layouts.SelectMany(l => l.Edges.Select(e => (Layout: l, Edge: e)));
        foreach (var duplicateGroup in edges.GroupBy(x => x.Edge.EdgeId).Where(g => g.Count() > 1))
        {
            string message = $"EdgeId '{duplicateGroup.Key}' duplicated";
            var failure = new ValidationFailure { ErrorMessage = message, CustomState = new LifValidationResult(ResourceType.Edge, duplicateGroup.Key, Models.Lif.Severity.Err, message) };
            context.AddFailure(failure);
        }

        var nodes = layouts.SelectMany(l => l.Nodes.Select(n => (Layout: l, Node: n)));
        foreach (var duplicateGroup in nodes.GroupBy(x => x.Node.NodeId).Where(g => g.Count() > 1))
        {
            string message = $"NodeId '{duplicateGroup.Key}' duplicated";
            var failure = new ValidationFailure { ErrorMessage = message, CustomState = new LifValidationResult(ResourceType.Node, duplicateGroup.Key, Models.Lif.Severity.Err, message) };
            context.AddFailure(failure);
        }
    }
}