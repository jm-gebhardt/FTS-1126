using System.Numerics;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Graphs;

namespace Gebhardt.Acc.DeviceCommunication.SimAgv.Extensions;

/// <summary>
/// Provides extension methods for <see cref="ExportedControlPoint"/>
/// </summary>
public static class ExportedControlPointExtension
{
    /// <summary>
    /// Extension block for <see cref="ExportedControlPoint"/>.
    /// </summary>
    extension(ExportedControlPoint point)
    {
        /// <summary>
        /// Get the <see cref="Vector2"/> representation of a control point.
        /// </summary>
        /// <returns>Vector to using x, y coordinates of the control point.</returns>
        public Vector2 AsVector2()
        {
            return new Vector2((float)point.X, (float)point.Y);
        }

        /// <summary>
        /// Convert to <see cref="EdgeCoefficient"/>
        /// </summary>
        /// <returns></returns>
        public EdgeCoefficient ToEdgeCoefficient()
        {
            return new EdgeCoefficient(point.X, point.Y);
        }
    }
}