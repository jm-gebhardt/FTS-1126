using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using System.Numerics;
using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Travel;

/// <summary>
/// Provides an abstract base class for edge travel calculations.
/// </summary>
public abstract class BaseEdge : GraphEdge
{
    /// <summary>
    /// The time the agv enters the edge.
    /// </summary>
    public DateTime EntryTime { get; protected set; }

    /// <summary>
    /// The time the initial turn into the edge is finished.
    /// </summary>
    public DateTime InitialTurnFinishTime { get; protected set; }

    /// <summary>
    /// The initial velocity of the agv during entry [m/s].
    /// </summary>
    public double InitialVelocity { get; protected set; }

    /// <summary>
    /// The angle of the agv right before entering the edge [rad].
    /// </summary>
    public double EntryAngle { get; protected set; }

    /// <summary>
    /// The target angle when the agv exits from the edge [rad].
    /// </summary>
    public double ExitAngle { get; protected set; }

    /// <summary>
    /// The maximum allowed velocity on the edge.
    /// </summary>
    protected double MaxVelocity { get; }

    /// <summary>
    /// The estimated time when the agv exits from the edge.
    /// </summary>
    public DateTime EstimatedExitTime { get; protected set; }

    /// <summary>
    /// The velocity of the agv during exit [m/s].
    /// </summary>
    public double ExitVelocity { get; protected set; }

    /// <summary>
    /// The source node of the edge.
    /// </summary>
    public GraphNode SourceNode { get; }

    /// <summary>
    /// The target node of the edge.
    /// </summary>
    public GraphNode TargetNode { get; }

    /// <summary>
    /// The first target node of the first section of the edge.
    /// </summary>
    public GraphNode ReferenceTarget { get; protected init; }

    /// <summary>
    /// The agv orientation [rad] on the edge.
    /// </summary>
    public float AgvOrientation { get; protected init; }

    /// <inheritdoc cref="BaseEdge"/>>
    protected BaseEdge(GraphEdge edge, GraphNode sourceNode, GraphNode targetNode) : base(edge.FromNodeId, edge.ToNodeId, (float)edge.Length, edge.Id, edge.Variant, edge.VariantParameters)
    {
        SourceNode = sourceNode;
        TargetNode = targetNode;
        ReferenceTarget = targetNode;
        MaxVelocity = 1000;
        AgvOrientation = GetAgvOrientation(sourceNode, targetNode);
    }

    /// <summary>
    /// Set <see cref="EntryAngle"/> and <see cref="ExitAngle"/> of the edge.
    /// </summary>
    public virtual void SetAngles(double entryAngle, GraphNode? nextSource, GraphNode? nextTarget)
    {
        EntryAngle = entryAngle;
        if (nextTarget != null && nextSource != null)
        {
            ExitAngle = GetExitAngle(nextSource, nextTarget);
        }
    }

    private double GetExitAngle(GraphNode nextSourceNode, GraphNode nextTargetNode)
    {
        var sourcePosition = SourceNode.AsVector2();
        var targetPosition = TargetNode.AsVector2();
        var tangentOnCurrentEdge = targetPosition - sourcePosition;

        var nextSourcePosition = nextSourceNode.AsVector2();
        var nextTargetPosition = nextTargetNode.AsVector2();
        var tangentOnNextEdge = nextTargetPosition - nextSourcePosition;

        return Mathematics.AngleBetween(tangentOnCurrentEdge, tangentOnNextEdge);
    }

    /// <summary>
    /// Gets the partial orientation of the agv during turning.
    /// </summary>
    /// <param name="now">The current timestamp.</param>
    /// <param name="initialTurnTime">The time the initial turn is finished.</param>
    /// <param name="entryTime">The entry time to the edge.</param>
    /// <param name="orientation">The orientation on the edge.</param>
    /// <param name="previousOrientation">The orientation on the previous edge.</param>
    /// <returns></returns>
    public static float GetPartialOrientation(DateTime now, DateTime initialTurnTime, DateTime entryTime, float orientation, float previousOrientation)
    {
        if (now >= initialTurnTime)
        {
            return orientation;
        }
        double secondsSinceEntry = (now - entryTime).TotalSeconds;
        double secondsToTurn = (initialTurnTime - entryTime).TotalSeconds;
        double turningFraction = Math.Abs(secondsToTurn) > double.Epsilon ? secondsSinceEntry / secondsToTurn : 0;

        double angle = (orientation - previousOrientation) % (2d * Math.PI);
        if (Math.Abs(angle) > Math.PI)
        {
            angle -= Math.Sign(angle) * 2d * Math.PI;
        }

        orientation = (previousOrientation + (float)(angle * turningFraction)) % (float)(2d * Math.PI);

        if (orientation < 0)
        {
            orientation += (float)(2d * Math.PI);
        }
        if (orientation > 2f * Math.PI)
        {
            orientation -= (float)(2d * Math.PI);
        }
        return orientation;
    }

    /// <summary>
    /// Estimates the time the agv travels on the edge.
    /// </summary>
    public abstract void EstimateTravelTime(AgvData agvData, double initialVelocity, DateTime entryTime);

    /// <summary>
    /// Gets the current position of the agv on the edge.
    /// </summary>
    public abstract AgvPosition GetCurrentPosition(DateTime now, AgvData agvData, float previousOrientation);

    /// <summary>
    /// Gets the agv orientation [rad].
    /// </summary>
    public static float GetAgvOrientation(GraphNode source, GraphNode target)
    {
        var fromVector = source.AsVector2();
        var toVector = target.AsVector2();
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

    /// <summary>
    /// Gets the agv orientation [rad].
    /// </summary>
    public static float GetAgvOrientation(Vector2 fromVector, Vector2 toVector)
    {
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