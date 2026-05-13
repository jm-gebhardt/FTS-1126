using System.Numerics;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Services;
using Gebhardt.Acc.Common.Domain.Utilities;
using JetBrains.Annotations;

// ReSharper disable InconsistentNaming
namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models.Orders;

/// <summary>
/// Directional connection between two nodes to be traversed for fulfilling an order.
/// </summary>
[PublicAPI]
public class OrderEdge
{
    private Bezier _bezier = null!;

    /// <summary>
    /// Unique edge identification.
    /// </summary>
    public string EdgeId { get; set; } = string.Empty;

    /// <summary>
    /// Number to track the sequence of nodes and edges in an order and to simplify order updates.
    /// Runs across all nodes and edges of the same order and is reset when a new orderId is issued.
    /// </summary>
    public int SequenceId { get; set; }

    /// <summary>
    /// Additional information on the edge.
    /// </summary>
    public string? EdgeDescription { get; set; }

    /// <summary>
    /// True indicates that the edge is part of the base.
    /// False indicates that the edge is part of the horizon.
    /// </summary>
    public bool Released { get; set; }

    /// <summary>
    /// The nodeId of the start node.
    /// </summary>
    public string StartNodeId { get; set; } = string.Empty;

    /// <summary>
    /// The nodeId of the end node.
    /// </summary>
    public string EndNodeId { get; set; } = string.Empty;

    /// <summary>
    /// Permitted maximum speed on the edge in m/s. Optional: no limit if not set.
    /// </summary>
    public double? MaxSpeed { get; set; }

    /// <summary>
    /// Permitted maximum height of the vehicle including the load on the edge in meters.
    /// Optional: no limit if not set.
    /// </summary>
    public double? MaxHeight { get; set; }

    /// <summary>
    /// Permitted minimum height of the load handling device on the edge in meters.
    /// Optional: no limit if not set.
    /// </summary>
    public double? MinHeight { get; set; }

    /// <summary>
    /// Orientation of the AGV on the edge in radians [-π, π].
    /// Optional.
    /// </summary>
    public double? Orientation { get; set; }

    /// <summary>
    /// Defines whether orientation is interpreted relative to the global map coordinate system or tangential to the edge.
    /// If not defined, the default value is <see cref="OrientationType.TANGENTIAL"/>.
    /// </summary>
    public OrientationType? OrientationType { get; set; }

    /// <summary>
    /// Sets direction at junctions for line-guided or wire-guided vehicles.
    /// Optional.
    /// </summary>
    public string? Direction { get; set; }

    /// <summary>
    /// True: rotation is allowed on the edge. False: rotation is not allowed on the edge.
    /// Optional: no limit if not set.
    /// </summary>
    public bool? RotationAllowed { get; set; }

    /// <summary>
    /// Maximum rotation speed in rad/s. Optional: no limit if not set.
    /// </summary>
    public double? MaxRotationSpeed { get; set; }

    /// <summary>
    /// Distance of the path from startNode to endNode in meters.
    /// Optional: used by line-guided AGVs to decrease speed before reaching a stop position.
    /// </summary>
    public double? Length { get; set; }

    /// <summary>
    /// Trajectory as a NURBS for this edge.
    /// Optional: can be omitted if the AGV plans its own trajectory.
    /// </summary>
    public Trajectory? Trajectory { get; set; }

    /// <summary>
    /// Boundaries within which the vehicle can deviate from its trajectory, e.g., to avoid obstacles.
    /// Optional.
    /// </summary>
    public Corridor? Corridor { get; set; }

    /// <summary>
    /// Array of actions to be executed on this edge.
    /// </summary>
    public ICollection<OrderAction> Actions { get; set; } = [];

    /// <summary>
    /// The ids of the actions of the edge.
    /// </summary>
    public ICollection<string> ActionIds => Actions.Select(a => a.ActionId).ToList();

    /// <summary>
    /// The graph edge representation.
    /// </summary>
    public GraphEdge GraphEdge { get; set; } = null!;

    /// <summary>
    /// The estimated travel time
    /// </summary>
    public TimeSpan EstimatedTravelTime { get; private set; }

    /// <summary>
    /// Release the edge.
    /// </summary>
    public void Release()
    {
        Released = true;
    }

    private static readonly TimeSpan FallbackTravelTime = TimeSpan.FromSeconds(1);

    private static TimeSpan EstimateFromLength(double? length, float agvSpeed) => length is > 0 && agvSpeed > 0 ?
        TimeSpan.FromSeconds(length.Value / agvSpeed) :
        FallbackTravelTime;

    /// <summary>
    /// Prepare the edge for travel
    /// </summary>
    public void Prepare(INurbsEdgeBuilder edgeBuilder, float agvSpeed)
    {
        var nurbs = Trajectory?.ToNurbs();
        if (Trajectory == null || nurbs == null)
        {
            EstimatedTravelTime = EstimateFromLength(Length, agvSpeed);
            return;
        }
        List<Vector2> controlPoints = Trajectory.ControlPoints.Select(cp => new Vector2((float)cp.X, (float)cp.Y)).ToList();
        if (controlPoints.Count >= 3)
        {
            _bezier = new Bezier(controlPoints);
        }
        GraphEdge = edgeBuilder.BuildFromNurbs(nurbs, StartNodeId.ToGuid(), EndNodeId.ToGuid());
        if (GraphEdge.Length == 0)
        {
            EstimatedTravelTime = TimeSpan.FromSeconds(1);
            return;
        }
        EstimatedTravelTime = EstimateFromLength(GraphEdge.Length, agvSpeed);
    }

    /// <summary>
    /// Get the position of the agv on the edge
    /// </summary>
    /// <param name="travelledFraction">The fraction of the edge that has been travelled.</param>
    /// <returns>The current agv position.</returns>
    public AgvPosition GetPosition(double travelledFraction)
    {
        if (Trajectory == null)
        {
            return new AgvPosition() { X = 0, Y = 0, Theta = GetAgvOrientation(), Z = 0, PositionInitialized = true };
        }
        double interValPosition = Math.Clamp(travelledFraction, 0, 1);
        if (Trajectory?.ControlPoints.Count >= 3)
        {
            var position = _bezier.DeCasteljau(interValPosition, out _, out _);
            return new AgvPosition() { X = position.X, Y = position.Y, Theta = GetAgvOrientation(), Z = 0, PositionInitialized = true };
        }
        return GetPositionOnLinearEdge(interValPosition);
    }

    private AgvPosition GetPositionOnLinearEdge(double travelledFraction)
    {
        var sourceNode = Trajectory!.ControlPoints.First();
        var targetNode = Trajectory!.ControlPoints.Last();
        double length = Length ?? float.Epsilon;
        double travelDistance = length * travelledFraction;
        double deltaX = targetNode.X - sourceNode.X;
        float x, y;
        if (Math.Abs(deltaX) > float.Epsilon)
        {
            x = (float)(targetNode.X - (length - travelDistance) * deltaX / length);
            y = (float)targetNode.Y;
            if (Math.Abs(sourceNode.Y - targetNode.Y) > float.Epsilon)
            {
                y = (float)((targetNode.X - x) * (sourceNode.Y - targetNode.Y) / deltaX + targetNode.Y);
            }
        }
        else
        {
            x = (float)sourceNode.X;
            y = (float)((targetNode.Y - sourceNode.Y) * travelDistance / length + sourceNode.Y);
        }
        return new AgvPosition() { X = x, Y = y, Theta = GetAgvOrientation(), Z = 0, PositionInitialized = true };
    }

    private float GetAgvOrientation()
    {
        if (Orientation != null)
        {
            return (float)Orientation;
        }
        if (Trajectory == null)
        {
            return 0;
        }
        List<Vector2> controlPoints = Trajectory.ControlPoints.Select(cp => new Vector2((float)cp.X, (float)cp.Y)).ToList();
        var fromVector = controlPoints.First();
        var toVector = controlPoints.Last();
        if (toVector == Vector2.Zero && fromVector != Vector2.Zero)
        {
            // use tangent
            toVector -= fromVector;
        }
        if (fromVector == Vector2.Zero && toVector != Vector2.Zero)
        {
            // use tangent
            fromVector -= toVector;
        }
        return (float)(Math.Atan2(toVector.Y - fromVector.Y, toVector.X - fromVector.X) + Math.PI / 2);
    }
}

/// <summary>
/// Orientation type for an edge.
/// </summary>
[PublicAPI]
public enum OrientationType
{
    /// <summary>Orientation is relative to the global project-specific map coordinate system.</summary>
    GLOBAL,

    /// <summary>Orientation is tangential to the edge. 0.0 = forwards, π = backwards.</summary>
    TANGENTIAL
}