using Gebhardt.Acc.Statistics.Domain.Models;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Statistics.WebApi.Dtos;

/// <summary>
/// Provides a dto for <see cref="Grid"/>.
/// </summary>
public class GridDto
{
    /// <summary>
    /// The uid of the grid.
    /// </summary>
    public Guid GridId { get; init; }

    /// <summary>
    /// The display name of the grid.
    /// </summary>
    public string GridName { get; init; } = string.Empty;

    /// <summary>
    /// The starting position of the grid in x-direction.
    /// </summary>
    public float XStart { get; private set; }

    /// <summary>
    /// The starting position of the grid in y-direction.
    /// </summary>
    public float YStart { get; private set; }

    /// <summary>
    /// The grid cell width in x-direction.
    /// </summary>
    public float XStep { get; init; }

    /// <summary>
    /// The grid cell width in y-direction.
    /// </summary>
    public float YStep { get; init; }

    /// <summary>
    /// The grid size in x-direction.
    /// </summary>
    public int XSize { get; private set; }

    /// <summary>
    /// The grid size in y-direction.
    /// </summary>
    public int YSize { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="gridName"></param>
    /// <param name="ySize"></param>
    /// <param name="xSize"></param>
    /// <param name="xStart"></param>
    /// <param name="xStep"></param>
    /// <param name="yStart"></param>
    /// <param name="yStep"></param>
    public GridDto(string gridName, int ySize, int xSize, float xStart, float xStep, float yStart, float yStep)
    {
        GridId = Guid.NewGuid();
        GridName = gridName;
        XSize = xSize;
        YSize = ySize;
        XStart = xStart;
        XStep = xStep;
        YStart = yStart;
        YStep = yStep;
    }

    /// <summary>
    /// AutoMapper
    /// </summary>
    [UsedImplicitly]
    private GridDto()
    {
    }
}