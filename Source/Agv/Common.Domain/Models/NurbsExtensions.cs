using System.Numerics;
using Gebhardt.Acc.Common.Domain.Models.Graphs;

namespace Gebhardt.Acc.Common.Domain.Models;

/// <summary>
/// Provides extension methods for NURBS.
/// </summary>
public static class NurbsExtensions
{
    extension(NurbsControlPoint controlPoint)
    {
        /// <summary>
        /// Convert the control point to <see cref="Vector3"/> using z=0.
        /// </summary>
        public Vector3 AsVector3()
        {
            return new Vector3((float)controlPoint.X, (float)controlPoint.Y, 0);
        }
    }
}