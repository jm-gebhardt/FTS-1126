using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.NewGraph;
using JetBrains.Annotations;

// ReSharper disable InconsistentNaming
namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models.Orders;

/// <summary>
/// Directional connection between two nodes to be traversed for fulfilling an order.
/// </summary>
[PublicAPI]
public class OrderEdge
{
    private static readonly TimeSpan FallbackTravelTime = TimeSpan.FromSeconds(1);

    private static TimeSpan EstimateFromLength(double length, double agvSpeed) => length > 0 && agvSpeed > 0 ?
        TimeSpan.FromSeconds(length / agvSpeed) :
        FallbackTravelTime;

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

    /// <summary>
    /// Prepare the edge for travel
    /// </summary>
    public void Prepare(float maxAgvSpeed, OrderNode startNode, OrderNode endNode)
    {
        bool isRotationOnSpot = false;
        double length = Trajectory?.CalculateLength() ?? GetLengthOfStraightEdge(startNode, endNode, out isRotationOnSpot);
        if (!isRotationOnSpot)
        {
            double agvSpeed = MaxSpeed ?? maxAgvSpeed;
            EstimatedTravelTime = EstimateFromLength(length, agvSpeed);
            return;
        }

        // TODO kua: use rotation angle and rotation speed to calculate time
        EstimatedTravelTime = FallbackTravelTime;
    }
    
    /// <summary>
    /// Get the position of the agv on the edge
    /// </summary>
    /// <param name="travelledFraction">The fraction of the edge that has been travelled.</param>
    /// <param name="startNode">The edge's startNode</param>
    /// <param name="endNode">The edge's endNode</param>
    /// <returns>The current agv position.</returns>
    public AgvPosition GetPosition(double travelledFraction, OrderNode startNode, OrderNode endNode)
    {
        double travelledFractionInPercent = travelledFraction * 100;
        if (Trajectory != null)
        {
            var position = Trajectory.CalculatePositionAtDistancePercent(travelledFractionInPercent, out double edgeOrientation);
            double orientation = GetOrientation(edgeOrientation);
            return new AgvPosition { X = (float)position.X, Y = (float)position.Y, Theta = (float)orientation, Z = 0, PositionInitialized = true };
        }
        else
        {
            var position = GetPositionOnStraightEdge(travelledFractionInPercent, startNode, endNode, out double edgeOrientation);
            double orientation = GetOrientation(edgeOrientation);
            return new AgvPosition { X = (float)position.X, Y = (float)position.Y, Theta = (float)orientation, Z = 0, PositionInitialized = true };
        }
    }

    private double GetOrientation(double currentEdgeOrientation)
    {
        double orientation = Orientation ?? 0;
        var orientationType = OrientationType ?? Orders.OrientationType.TANGENTIAL;
        
        return orientationType == Orders.OrientationType.GLOBAL ? orientation : currentEdgeOrientation + orientation;
    }

    private PolygonPoint GetPositionOnStraightEdge(double percent, OrderNode startNode, OrderNode endNode, out double edgeOrientation)
    {
        var startNodePosition = ToPolygonPoint(startNode.NodePosition);
        var endNodePosition = ToPolygonPoint(endNode.NodePosition);
        var vector = endNodePosition - startNodePosition;
        
        // use start and end angle to calculate orientation 
        edgeOrientation = vector.Angle;
        return startNodePosition + vector*(percent/100);
    }

    private double GetLengthOfStraightEdge(OrderNode startNode, OrderNode endNode, out bool isRotationOnSpot)
    {
        var startNodePosition = ToPolygonPoint(startNode.NodePosition);
        var endNodePosition = ToPolygonPoint(endNode.NodePosition);
        isRotationOnSpot = startNodePosition == endNodePosition;
        if (isRotationOnSpot)
        {
            return 0;
        }

        return (endNodePosition - startNodePosition).Length;
    }

    private static PolygonPoint ToPolygonPoint(OrderNodePosition? position)
    {
        return position == null ? new PolygonPoint(0, 0) : position.ToPolygonPoint();
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