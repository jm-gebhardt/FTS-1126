using System.Text.Json;
using Gebhardt.Acc.Vda5050Simulator.Domain;
using Gebhardt.Acc.Vda5050Simulator.Messaging.Dtos;

namespace Gebhardt.Acc.Vda5050Simulator.Messaging;

public sealed record MappingResult<T>(T? Value, IReadOnlyList<string> Errors) where T : class
{
    public bool IsValid => Value != null && Errors.Count == 0;
}

/// <summary>
/// Maps VDA5050 wire DTOs to domain types and validates structure (not state-dependent rules).
/// </summary>
public static class Vda5050Mapper
{
    public static MappingResult<Order> MapOrder(OrderDto dto)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(dto.OrderId))
        {
            errors.Add("orderId is missing");
        }
        if (dto.OrderUpdateId is null or < 0)
        {
            errors.Add("orderUpdateId is missing or negative");
        }
        if (dto.Nodes is not { Count: > 0 })
        {
            errors.Add("order contains no nodes");
        }

        var nodes = new List<OrderNode>();
        foreach (var node in dto.Nodes ?? [])
        {
            if (string.IsNullOrWhiteSpace(node.NodeId))
            {
                errors.Add($"node with sequenceId {node.SequenceId} has no nodeId");
                continue;
            }
            if (node.SequenceId < 0)
            {
                errors.Add($"node {node.NodeId}: negative sequenceId");
            }
            var position = MapPosition(node.NodePosition, node.NodeId, errors);
            if (position == null)
            {
                continue;
            }
            nodes.Add(new OrderNode(
                node.NodeId,
                node.SequenceId,
                node.Released,
                position,
                MapActions(node.Actions, $"node {node.NodeId}", errors)));
        }

        var nodeIds = nodes.Select(n => n.NodeId).ToHashSet(StringComparer.Ordinal);
        var edges = new List<OrderEdge>();
        foreach (var edge in dto.Edges ?? [])
        {
            if (string.IsNullOrWhiteSpace(edge.EdgeId))
            {
                errors.Add($"edge with sequenceId {edge.SequenceId} has no edgeId");
                continue;
            }
            if (edge.SequenceId < 0)
            {
                errors.Add($"edge {edge.EdgeId}: negative sequenceId");
            }
            if (string.IsNullOrWhiteSpace(edge.StartNodeId) || string.IsNullOrWhiteSpace(edge.EndNodeId))
            {
                errors.Add($"edge {edge.EdgeId}: startNodeId or endNodeId is missing");
                continue;
            }
            if (!nodeIds.Contains(edge.StartNodeId) || !nodeIds.Contains(edge.EndNodeId))
            {
                errors.Add($"edge {edge.EdgeId}: start or end node is not part of the order");
            }
            if (edge.MaxSpeed is <= 0)
            {
                errors.Add($"edge {edge.EdgeId}: maxSpeed must be positive");
            }
            edges.Add(new OrderEdge(
                edge.EdgeId,
                edge.SequenceId,
                edge.Released,
                edge.StartNodeId,
                edge.EndNodeId,
                edge.MaxSpeed,
                MapTrajectory(edge.Trajectory, edge.EdgeId, errors),
                MapActions(edge.Actions, $"edge {edge.EdgeId}", errors)));
        }

        var orderedNodes = nodes.OrderBy(n => n.SequenceId).ToList();
        var orderedEdges = edges.OrderBy(e => e.SequenceId).ToList();

        if (errors.Count == 0)
        {
            ValidateSequence(orderedNodes, orderedEdges, errors);
        }

        if (errors.Count > 0)
        {
            return new MappingResult<Order>(null, errors);
        }

        var order = new Order(dto.OrderId!, dto.OrderUpdateId!.Value, orderedNodes, orderedEdges);
        return new MappingResult<Order>(order, errors);
    }

    public static MappingResult<IReadOnlyList<InstantAction>> MapInstantActions(InstantActionsDto dto)
    {
        var errors = new List<string>();
        if (dto.Actions is not { Count: > 0 })
        {
            errors.Add("instantActions contains no actions");
        }
        var actions = MapActions(dto.Actions, "instantAction", errors);
        if (errors.Count > 0)
        {
            return new MappingResult<IReadOnlyList<InstantAction>>(null, errors);
        }
        IReadOnlyList<InstantAction> result = actions.Select(a => new InstantAction(a)).ToList();
        return new MappingResult<IReadOnlyList<InstantAction>>(result, errors);
    }

    /// <summary>
    /// Nodes and edges must alternate with consecutive sequenceIds (node even, edge odd), ids must be unique and
    /// each edge must connect its neighbouring nodes. The first sequenceId may be greater than 0 (order update).
    /// </summary>
    private static void ValidateSequence(List<OrderNode> nodes, List<OrderEdge> edges, List<string> errors)
    {
        if (edges.Count != nodes.Count - 1)
        {
            errors.Add($"expected {nodes.Count - 1} edges for {nodes.Count} nodes but got {edges.Count}");
            return;
        }

        int firstSequenceId = nodes[0].SequenceId;
        if (firstSequenceId % 2 != 0)
        {
            errors.Add($"sequenceId of the first node must be even but is {firstSequenceId}");
            return;
        }

        foreach (var duplicate in nodes.GroupBy(n => n.SequenceId).Where(g => g.Count() > 1))
        {
            errors.Add($"sequenceId {duplicate.Key} is used by more than one node");
        }
        foreach (var duplicate in edges.GroupBy(e => e.SequenceId).Where(g => g.Count() > 1))
        {
            errors.Add($"sequenceId {duplicate.Key} is used by more than one edge");
        }
        foreach (var duplicate in nodes.GroupBy(n => n.NodeId).Where(g => g.Count() > 1))
        {
            errors.Add($"nodeId {duplicate.Key} is used by more than one node");
        }
        foreach (var duplicate in edges.GroupBy(e => e.EdgeId).Where(g => g.Count() > 1))
        {
            errors.Add($"edgeId {duplicate.Key} is used by more than one edge");
        }
        if (errors.Count > 0)
        {
            return;
        }

        for (int i = 0; i < nodes.Count; i++)
        {
            int expectedNode = firstSequenceId + 2 * i;
            if (nodes[i].SequenceId != expectedNode)
            {
                errors.Add($"node {nodes[i].NodeId}: sequenceId is {nodes[i].SequenceId} but {expectedNode} is expected");
            }
            if (i < edges.Count)
            {
                var edge = edges[i];
                int expectedEdge = expectedNode + 1;
                if (edge.SequenceId != expectedEdge)
                {
                    errors.Add($"edge {edge.EdgeId}: sequenceId is {edge.SequenceId} but {expectedEdge} is expected");
                }
                if (edge.StartNodeId != nodes[i].NodeId || edge.EndNodeId != nodes[i + 1].NodeId)
                {
                    errors.Add($"edge {edge.EdgeId}: must connect node {nodes[i].NodeId} and node {nodes[i + 1].NodeId}");
                }
            }
        }
    }

    private static NodePosition? MapPosition(NodePositionDto? dto, string nodeId, List<string> errors)
    {
        if (dto == null)
        {
            errors.Add($"node {nodeId}: nodePosition is missing");
            return null;
        }
        if (string.IsNullOrWhiteSpace(dto.MapId))
        {
            errors.Add($"node {nodeId}: nodePosition.mapId is missing");
            return null;
        }
        return new NodePosition(dto.X, dto.Y, dto.Theta, dto.AllowedDeviationXY, dto.MapId);
    }

    private static List<AgvAction> MapActions(List<ActionDto>? dtos, string owner, List<string> errors)
    {
        var result = new List<AgvAction>();
        foreach (var dto in dtos ?? [])
        {
            if (string.IsNullOrWhiteSpace(dto.ActionId) || string.IsNullOrWhiteSpace(dto.ActionType))
            {
                errors.Add($"{owner}: action without actionId or actionType");
                continue;
            }
            if (!Enum.TryParse(dto.BlockingType, ignoreCase: true, out BlockingType blocking))
            {
                errors.Add($"{owner}: action {dto.ActionId} has invalid blockingType '{dto.BlockingType}'");
                continue;
            }
            var parameters = (dto.ActionParameters ?? [])
                .Where(p => !string.IsNullOrWhiteSpace(p.Key))
                .Select(p => new ActionParameter(p.Key!, ValueToString(p.Value)))
                .ToList();
            result.Add(new AgvAction(dto.ActionId, dto.ActionType, blocking, dto.ActionDescription, parameters));
        }
        return result;
    }

    private static string ValueToString(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString() ?? string.Empty,
        JsonValueKind.Undefined or JsonValueKind.Null => string.Empty,
        _ => value.GetRawText()
    };

    private static NurbsTrajectory? MapTrajectory(TrajectoryDto? dto, string edgeId, List<string> errors)
    {
        if (dto == null)
        {
            return null;
        }
        var knots = dto.KnotVector ?? [];
        var points = dto.ControlPoints ?? [];
        bool valid = true;

        if (dto.Degree < 1)
        {
            errors.Add($"edge {edgeId}: trajectory degree must be >= 1");
            valid = false;
        }
        if (points.Count <= dto.Degree)
        {
            errors.Add($"edge {edgeId}: trajectory needs more than {dto.Degree} control points");
            valid = false;
        }
        if (valid && knots.Count != points.Count + dto.Degree + 1)
        {
            errors.Add($"edge {edgeId}: knotVector must contain {points.Count + dto.Degree + 1} values but has {knots.Count}");
            valid = false;
        }
        if (valid && knots.Zip(knots.Skip(1), (a, b) => b < a).Any(decreasing => decreasing))
        {
            errors.Add($"edge {edgeId}: knotVector must be non-decreasing");
            valid = false;
        }
        if (points.Any(p => p.Weight is <= 0))
        {
            errors.Add($"edge {edgeId}: control point weights must be positive");
            valid = false;
        }

        return valid
            ? new NurbsTrajectory(dto.Degree, knots, points.Select(p => new ControlPoint(p.X, p.Y, p.Weight ?? 1.0)).ToList())
            : null;
    }
}
