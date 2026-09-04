using Gebhardt.Acc.Common.Domain.Models.Graphs;
using SkiaSharp;

namespace Gebhardt.Acc.Common.Domain.Services;

/// <summary>
/// Generates PNG map for simulation purpose based on a given graph.
/// <para>
/// Image width is a fixed value of <see cref="GeneratedMapGenerator.ImageWidth"/>.
///      |---- <see cref="GeneratedMapGenerator.ImageWidth"/> px ------|
///                                                     |--3m-|
///       ----------------------------------------------------   ---
///      |                                                    |   | 3
///      |                                                    |   | m
///      | minX/minY [m]-----------------------o--------      |  ---
///      |     |                                        |     |
///      |     |                       o                |     |
///      |     |    0/0 [m]                             |     |
///      |     |                                        |     |
///      |     |           o                            |     |
///      |     |                             o          |     |
///      |     o                                        |     |
///      |     |              o                         |     |
///      |     |                                        o     |
///      |     |                                        |     |
///      |     |                     o                  |     |
///      |      -----------o------------------------maxX/maxY |
///      |                                                    |
///      |                  Simulated map powered by Gebhardt |
///       ----------------------------------------------------
///
/// The outer bounds of the inner area of the image are derived from the min (minXY) and max (maxXY) coordinates of the nodes (o).
/// The resolution is r = totalWidth[m] / ImageWidth
/// The offset is the distance [m] from the left and upper corner to the origin (0/0).
/// </para>
/// </summary>
public class GeneratedMapGenerator : MapGeneratorBase{
    /// <summary>
    /// Fixed image width.
    /// </summary>
    private const int ImageWidth = 1440;

    /// <inheritdoc />
    public override bool CanBeUsed(string mapName) => true;

    private readonly TimeProvider _timeProvider;

    /// <inheritdoc cref="GeneratedMapGenerator"/>
    public GeneratedMapGenerator(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">Graph nodes is empty.</exception>
    protected override Map GenerateInternal(Guid mapId, string mapName, IGraph graph, (float X, float Y) min, (float X, float Y) max)
    {
        float resolution = (max.X - min.X) / ImageWidth;
        float xOffset = - min.X;
        float yOffset = - min.Y;

        int imageHeightPx = (int)Math.Ceiling((max.Y - min.Y) / resolution);

        var imageInfo = new SKImageInfo(ImageWidth, imageHeightPx);
        using var surface = SKSurface.Create(imageInfo);
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        byte[]? map = data.ToArray();
        return new Map(
            mapId,
            mapName,
            _timeProvider.GetUtcNow().UtcDateTime,
            Convert.ToBase64String(map),
            ImageWidth,
            imageHeightPx,
            resolution,
            xOffset,
            yOffset,
            graph.Nodes,
            graph.Edges);
    }
}