using System.Numerics;
using Gebhardt.StoreWare.Agv.Common.Utilities;

namespace Gebhardt.StoreWare.Agv.Common.Models.Graphs;

/// <summary>
/// Represents a Slanted edge.
/// </summary>
public class Slanted
{
    /// <summary>
    /// The exit angle of the node.
    /// </summary>
    private double Angle { get; }

    /// <summary>
    /// The distance of the slant start/end.
    /// </summary>
    private double Distance { get; }

    /// <summary>
    /// The start point of the edge.
    /// </summary>
    private Vector3 Start { get; }

    /// <summary>
    /// The end of the edge.
    /// </summary>
    private Vector3 End { get; }

    /// <summary>
    /// Initializes a new <see cref="Slanted"/> instance.
    /// </summary>
    /// <param name="start">The start of the edge.</param>
    /// <param name="end">The end of the edge.</param>
    /// <param name="angle">The angle to exit start on.</param>
    /// <param name="distance">The distance until the slant.</param>
    public Slanted(Vector3 start, Vector3 end,double angle, double distance)
    {
        Start = start;
        End = end;
        Angle = angle;
        Distance = distance;
    }

    /// <summary>
    /// Gets the length of his <see cref="Slanted"/>.
    /// </summary>
    /// <returns></returns>
    public double GetLength() {
        double length = Distance * 2;
        double outAngle = Math.PI + Angle;
        var from = Mathematics.RotateMoveVector3(Start, Angle, Distance);
        var to = Mathematics.RotateMoveVector3(End, outAngle, Distance);
        length += Mathematics.EuclideanDistance(from, to);
        return length;
    }
}