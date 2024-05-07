using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;

namespace Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Travel;

/// <summary>
/// Represents a <see cref="BaseEdge"/> of type <see cref="EdgeType.Direct"/>.
/// </summary>
public class DirectEdge : SimpleEdge
{
    /// <inheritdoc cref="DirectEdge"/>
    public DirectEdge(GraphEdge edge, GraphNode sourceNode, GraphNode targetNode, bool agvGoesBackwards = false) : base(edge, sourceNode, targetNode, agvGoesBackwards)
    {
    }

    /// <inheritdoc cref="BaseEdge"/>
    public override AgvPosition GetCurrentPosition(DateTime now, AgvData agvData, float previousOrientation)
    {
        double travelDistance = GetEdgeTravelDistance(agvData, now);
        (float x, float y, float z) = GetPositionFromDistance(travelDistance);
        float theta = GetPartialOrientation(now, InitialTurnFinishTime, EntryTime, AgvOrientation, previousOrientation);
        return new AgvPosition(true, x, y, z, theta);
    }

    /// <summary>
    /// Get the agvs position on the edge based on the travel distance.
    /// </summary>
    /// <param name="travelDistance">The agv travel distance.</param>
    private (float x, float y, float z) GetPositionFromDistance(double travelDistance)
    {
        float x = SourceNode.X;
        float y = SourceNode.Y;
        float z = SourceNode.Z;
        if (!(travelDistance > double.Epsilon))
        {
            return (x, y, z);
        }
        // using interception theorem
        z = (float)((TargetNode.Z - SourceNode.Z) * TargetNode.Z * travelDistance / Length + SourceNode.Z);
        double deltaX = TargetNode.X - SourceNode.X;
        x = TargetNode.X;
        if (Math.Abs(deltaX) > float.Epsilon)
        {
            x = (float)(TargetNode.X - (Length - travelDistance) * deltaX / Length);
        }
        else
        {
            // using linear propagation
            y = (float)((TargetNode.Y - SourceNode.Y) * travelDistance / Length + SourceNode.Y);
            return (x, y, z);
        }

        y = TargetNode.Y;
        deltaX = Math.Abs(deltaX) > float.Epsilon ? deltaX : 1;
        if (Math.Abs(SourceNode.Y - TargetNode.Y) > float.Epsilon)
        {
            y = (float)((TargetNode.X - x) * (SourceNode.Y - TargetNode.Y) / deltaX + TargetNode.Y);
        }
        return (x, y, z);
    }
}