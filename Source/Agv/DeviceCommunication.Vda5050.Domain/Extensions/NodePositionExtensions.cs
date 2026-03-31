using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Extensions;

/// <summary>
/// Extension methods for <see cref="NodePosition"/>.
/// </summary>
[PublicAPI]
public static class NodePositionExtensions
{
    /// <summary>
    /// Creates a straight-line trajectory from this node position to the given end node position.
    /// </summary>
    public static Trajectory CreateLine(this NodePosition start, NodePosition end)
    {
        return new Trajectory
        {
            Degree = 1,
            KnotVector = [0.0f, 0.0f, 1.0f, 1.0f],
            ControlPoints =
            [
                new ControlPoint { X = start.X, Y = start.Y },
                new ControlPoint { X = end.X, Y = end.Y }
            ]
        };
    }
}

