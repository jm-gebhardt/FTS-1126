using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;

/// <summary>
/// Trajectory described as a NURBS.
/// </summary>
[PublicAPI]
public class Trajectory
{
    /// <inheritdoc cref="Trajectory"/>
    public Trajectory(ICollection<ControlPoint> controlPoints, double[] knotVector, int? degree = null)
    {
        ControlPoints = controlPoints;
        KnotVector = knotVector;
        Degree = degree;
    }

    /// <inheritdoc cref="Trajectory"/>
    public Trajectory()
    {
    }

    /// <summary>
    /// Control points defining the trajectory.
    /// </summary>
    public ICollection<ControlPoint> ControlPoints { get; set; } = [];

    /// <summary>
    /// Degree of the trajectory curve. Default is 1. Range: [1 ... 3]
    /// </summary>
    public int? Degree { get; set; }

    /// <summary>
    /// Knot vector for the trajectory.
    /// </summary>
    public double[] KnotVector { get; set; } = [];
}
