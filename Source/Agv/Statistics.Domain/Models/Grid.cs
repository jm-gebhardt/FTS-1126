using Gebhardt.Acc.Statistics.Domain.Exceptions;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Statistics.Domain.Models;

/// <summary>
/// Represents a grid.
/// </summary>
public class Grid(string gridName, int ySize, int xSize, double xStart, double xStep, double yStart, double yStep)
{
    /// <summary>
    /// The uid of the grid.
    /// </summary>
    public Guid GridId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// The display name of the grid.
    /// </summary>
    public string GridName { get; init; } = gridName;

    /// <summary>
    /// The starting position of the grid in x-direction.
    /// </summary>
    public double XStart { get; private set; } = xStart;

    /// <summary>
    /// The starting position of the grid in y-direction.
    /// </summary>
    public double YStart { get; private set; } = yStart;

    /// <summary>
    /// The grid cell width in x-direction.
    /// </summary>
    public double XStep { get; init; } = xStep;

    /// <summary>
    /// The grid cell width in y-direction.
    /// </summary>
    public double YStep { get; init; } = yStep;

    /// <summary>
    /// The grid size in x-direction.
    /// </summary>
    public int XSize { get; private set; } = xSize;

    /// <summary>
    /// The grid size in y-direction.
    /// </summary>
    public int YSize { get; private set; } = ySize;
    
    /// <summary>
    /// The list of AgvGridPositions related to this grid.
    /// </summary>
    [UsedImplicitly]
    public ICollection<GridAgvPosition>? AgvGridPositions { get; init; } = new List<GridAgvPosition>();
    
    /// <summary>
    /// The list of Maps related to this grid.
    /// </summary>
    [UsedImplicitly]
    public ICollection<Map>? Maps { get; init; } = new List<Map>();

    /// <summary>
    /// Get the maximum x and y values.
    /// </summary>
    /// <returns></returns>
    public (double xMax, double yMax) GetXyMax()
    {
        return (XStart + XStep * (XSize - 1), YStart + YStep * (YSize - 1));
    }

    /// <summary>
    /// Try to expand the grid based on the given grid.
    /// I.e. set new x or y start and x or y size if the given grid extends further.
    /// </summary>
    /// <param name="grid">The grid by which to expand.</param>
    /// <exception cref="IncompatibleGridException">If grid resolutions mismatch.</exception>
    public void TryExpandByGrid(Grid grid)
    {
        if (Math.Abs(grid.XStep - XStep) > double.Epsilon || Math.Abs(grid.YStep - YStep) > double.Epsilon)
        {
            throw new IncompatibleGridException();
        }
        (double xMax, double yMax) = GetXyMax();
        (double gridXMax, double gridYMax) = grid.GetXyMax();

        // Set new x and y start by full x or y steps, resp.ly.
        double xStartDiff = Math.Ceiling((XStart - grid.XStart) / XStep) * XStep;
        XStart -= Math.Max(xStartDiff, 0);
        double yStartDiff = Math.Ceiling((YStart - grid.YStart) / YStep) * YStep;
        YStart -= Math.Max(yStartDiff, 0);

        // Set new x and y size by full x or y steps, resp.ly.
        double xMaxDiff = Math.Ceiling((gridXMax - xMax) / XStep) * XStep;
        xMax += Math.Max(xMaxDiff, 0);
        double yMaxDiff = Math.Ceiling((gridYMax - yMax) / YStep) * YStep;
        yMax += Math.Max(yMaxDiff, 0);
        XSize = (int)Math.Ceiling((xMax - XStart + XStep) / XStep);
        YSize = (int)Math.Ceiling((yMax - YStart + YStep) / YStep);
    }

    /// <summary>
    /// Gets the x and y index.
    /// </summary>
    /// <param name="x">X position.</param>
    /// <param name="y">Y position.</param>
    /// <returns></returns>
    public (int xi, int yi) GetXyIndex(double x, double y)
    {
        return ((int)Math.Floor((x - XStart) / XStep + 1), (int)Math.Floor((y - YStart) / YStep + 1));
    }
}