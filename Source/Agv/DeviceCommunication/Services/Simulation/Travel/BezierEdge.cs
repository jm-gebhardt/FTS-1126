using System.Numerics;
using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Utilities;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Travel;

/// <summary>
/// Represents a <see cref="BaseEdge"/> of type <see cref="EdgeType.Bezier1"/> or <see cref="EdgeType.Bezier2"/> .
/// </summary>
public class BezierEdge : BaseEdge
{
    private readonly Bezier _bezier;

    private const float PositionCloseToSource = 0.05f;
    private const float PositionCloseToTarget = 0.95f;

    /// <inheritdoc cref="BezierEdge"/>
    public BezierEdge(GraphEdge edge, GraphNode sourceNode, GraphNode targetNode, bool agvGoesBackwards = false) : base(edge, sourceNode, targetNode)
    {
        var controlPolygon = new List<Vector2> { new(sourceNode.X, sourceNode.Y) };
        controlPolygon.AddRange(VariantParameters.Select(variantParameter => new Vector2((float)variantParameter.X!, (float)variantParameter.Y!)));
        controlPolygon.Add(new Vector2(targetNode.X, targetNode.Y));
        _bezier = new Bezier(controlPolygon);

        var sourceVector1 = _bezier.DeCasteljau(PositionCloseToSource, out _, out _);
        ReferenceTarget = new GraphNode("bezier") { X = sourceVector1.X, Y = sourceVector1.Y, Z = SourceNode.Z };

        var targetVector = _bezier.DeCasteljau(PositionCloseToTarget, out _, out _);
        AgvOrientation = agvGoesBackwards ? GetAgvOrientation(sourceVector1, sourceNode.AsVector2()) : GetAgvOrientation(targetVector, targetNode.AsVector2());
    }

    /// <inheritdoc cref="BaseEdge"/>
    public override void EstimateTravelTime(AgvData agvData, double initialVelocity, DateTime entryTime)
    {
        InitialVelocity = initialVelocity;
        EntryTime = entryTime;

        bool stopForTurnIntoEdge = EntryAngle >= agvData.TurnOnSpotThreshold && agvData.TurnOnSpotThreshold != 0;
        double time = 0;
        if (stopForTurnIntoEdge)
        {
            time += EntryAngle / agvData.MaxVelocityTurnOnSpot;
        }
        InitialTurnFinishTime = EntryTime.AddSeconds(time);

        double maxVelocity = Math.MinMagnitude(agvData.MaxVelocityTurn, MaxVelocity);

        double maxTime = PhysicsHelper.CalculateTimeToAccelerateAndMoveAtMaxVelocity(agvData.Acceleration, Length, maxVelocity, initialVelocity, out double finalVelocity);
        time += maxTime;

        EstimatedExitTime = EntryTime.AddSeconds(time);

        bool stopForTurnOutOfEdge = ExitAngle >= agvData.TurnOnSpotThreshold && agvData.TurnOnSpotThreshold != 0;
        ExitVelocity = stopForTurnOutOfEdge ? 0 : finalVelocity;
    }

    /// <inheritdoc cref="BaseEdge"/>>
    public override void SetAngles(double entryAngle, GraphNode? nextSource, GraphNode? nextTarget, bool turnAgvAtExit = false)
    {
        EntryAngle = entryAngle;
        if (nextTarget == null || nextSource == null)
        {
            return;
        }

        var positionCloseToTargetNode = _bezier.DeCasteljau(0.95, out _, out _);
        var positionAtTargetNode = TargetNode.AsVector2();
        var tangentOnCurrentEdge = positionAtTargetNode - positionCloseToTargetNode;

        var nextSourcePosition = nextSource.AsVector2();
        var nextTargetPosition = nextTarget.AsVector2();
        var tangentOnNextEdge = nextTargetPosition - nextSourcePosition;
        ExitAngle = Mathematics.AngleBetween(tangentOnCurrentEdge, tangentOnNextEdge);
        ExitAngle = ExitAngle == 0 && turnAgvAtExit ? Math.PI : ExitAngle;
    }

    /// <inheritdoc cref="BaseEdge"/>
    public override AgvPosition GetCurrentPosition(DateTime now, AgvData agvData, float previousOrientation)
    {
        double secondsOnEdge = (now - InitialTurnFinishTime).TotalSeconds;
        if (now < InitialTurnFinishTime)
        {
            secondsOnEdge = (now - EntryTime).TotalSeconds;
        }
        double edgeTravelTime = (EstimatedExitTime - InitialTurnFinishTime).TotalSeconds;
        double interValPosition = Math.Clamp(secondsOnEdge / edgeTravelTime, 0, 1);

        var position = _bezier.DeCasteljau(interValPosition, out var sourceVector, out _);
        float theta = GetAgvOrientation(sourceVector, position);
        theta = GetPartialOrientation(now, InitialTurnFinishTime, EntryTime, theta, previousOrientation);
        if (now >= InitialTurnFinishTime)
        {
            return new AgvPosition(true, position.X, position.Y, SourceNode.Z, theta);
        }
        position.X = SourceNode.X;
        position.Y = SourceNode.Y;
        return new AgvPosition(true, position.X, position.Y, SourceNode.Z, theta);
    }
}