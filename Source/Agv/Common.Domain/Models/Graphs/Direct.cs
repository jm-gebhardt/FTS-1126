using System.Numerics;
using Gebhardt.Acc.Common.Domain.Utilities;

namespace Gebhardt.Acc.Common.Domain.Models.Graphs;

/// <summary>
/// Represents a direct edge.
/// </summary>
public class Direct
{
    private Vector3 Start { get; }

    private Vector3 End { get; }

    /// <summary>
    /// Calculates the length of this Direct edge.
    /// </summary>
    public double GetLength() => Mathematics.EuclideanDistance(Start, End);

    /// <summary>
    /// Initializes a <see cref="Direct"/> edge instance.
    /// </summary>
    /// <param name="start">The start point of the edge.</param>
    /// <param name="end">The end point of the edge.</param>
    public Direct(Vector3 start, Vector3 end)
    {
        Start = start;
        End = end;
    }
}