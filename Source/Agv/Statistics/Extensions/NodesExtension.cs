using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Statistics.Domain.Models;

namespace Gebhardt.Acc.Statistics.Extensions;

/// <summary>
/// Provides extension methods to <see cref="GraphNode"/>
/// </summary>
public static class NodesExtension
{
    /// <summary>
    /// Convert to <see cref="Grid"/>.
    /// </summary>
    /// <param name="nodes">The graph nodes.</param>
    /// <param name="xStep">The x step size of the grid.</param>
    /// <param name="yStep">The y step size of the grid.</param>
    /// <param name="gridName">The name to be used for the grid.</param>
    /// <returns></returns>
    public static Grid ToGrid(this ICollection<GraphNode> nodes, double xStep, double yStep, string gridName)
    {
        double xStart = Math.Floor(nodes.MinBy(n => n.X)!.X);
        double yStart = Math.Floor(nodes.MinBy(n => n.Y)!.Y);
        double xMax = Math.Ceiling(nodes.MaxBy(n => n.X)!.X);
        double yMax = Math.Ceiling(nodes.MaxBy(n => n.Y)!.Y);

        int xSize = (int)Math.Ceiling((xMax - xStart + xStep) / xStep);
        int ySize = (int)Math.Ceiling((yMax - yStart + yStep) / yStep);

        return new Grid(gridName, ySize, xSize, xStart, xStep, yStart, yStep);
    }
}