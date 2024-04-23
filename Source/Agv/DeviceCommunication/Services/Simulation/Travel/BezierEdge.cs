using System.Numerics;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Extensions;

namespace Gebhardt.Acc.DeviceCommunication.Services.Simulation.Travel;

/// <summary>
/// Represents a <see cref="BaseEdge"/> of type <see cref="EdgeType.Bezier1"/> or <see cref="EdgeType.Bezier2"/> .
/// </summary>
public class BezierEdge : SimpleEdge
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

    /// <inheritdoc cref="SimpleEdge"/>>
    protected override double GetAgvMaxVelocity(AgvData agvData)
    {
        return agvData.MaxVelocityTurn;
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
        double travelDistance = GetEdgeTravelDistance(agvData, now);
        double interValPosition = Math.Clamp(travelDistance / Length, 0, 1);

        var position = _bezier.DeCasteljau(interValPosition, out _, out _);
        float theta = GetOrientation(now, interValPosition, previousOrientation);
        if (now >= InitialTurnFinishTime)
        {
            return new AgvPosition(true, position.X, position.Y, SourceNode.Z, theta);
        }
        position.X = SourceNode.X;
        position.Y = SourceNode.Y;
        return new AgvPosition(true, position.X, position.Y, SourceNode.Z, theta);
    }

    private float GetOrientation(DateTime now, double intervalPosition, float previousOrientation)
    {
        // ensure orientation is based on relative position on edge > 0
        intervalPosition = Math.Clamp(intervalPosition, 0.01, 1);
        var position = _bezier.DeCasteljau(intervalPosition, out var sourceVector, out _);
        float theta = GetAgvOrientation(sourceVector, position);
        return GetPartialOrientation(now, InitialTurnFinishTime, EntryTime, theta, previousOrientation);
    }
}