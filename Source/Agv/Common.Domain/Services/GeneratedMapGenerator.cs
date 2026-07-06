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
/// The outer area is extended for a fixed value in each direction (see <see cref="GeneratedMapGenerator.WallDistance"/>).
/// The resolution therefore is r = totalWidth[m] / ImageWidth
/// The offset is the distance [m] from the left and upper wall to the origin (0/0).
/// </para></summary>
public class GeneratedMapGenerator : MapGeneratorBase
{
    /// <summary>
    /// Image signature
    /// </summary>
    private const string Signature = "Simulated map powered by Gebhardt";

    /// <summary>
    /// Fixed image width.
    /// </summary>
    private const int ImageWidth = 1440;

    /// <summary>
    /// Fixed wall distance to inner area rectangular shaped by the nodes.
    /// </summary>
    private const float WallDistance = 3f;

    /// <summary>
    /// Fixed value for wall thickness.
    /// </summary>
    private const float WallThickness = 0.24f;

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
        float resolution = (max.X - min.X + 2 * WallDistance) / ImageWidth;
        float xOffset = WallDistance - min.X;
        float yOffset = WallDistance - min.Y;

        int imageHeightPx = (int)Math.Ceiling((max.Y - min.Y + 2 * WallDistance) / resolution);
        float wallThicknessPx = WallThickness / resolution; // Wall thickness: 24 cm

        var imageInfo = new SKImageInfo(ImageWidth, imageHeightPx);
        using var surface = SKSurface.Create(imageInfo);
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        using var paint1 = new SKPaint();
        paint1.Color = SKColors.DarkGray;
        paint1.IsAntialias = true;
        paint1.StrokeWidth = wallThicknessPx;
        paint1.Style = SKPaintStyle.Stroke;
        canvas.DrawRect(paint1.StrokeWidth, paint1.StrokeWidth, ImageWidth - 2 * paint1.StrokeWidth, imageHeightPx - 2 * paint1.StrokeWidth, paint1);

        using var paint2 = new SKPaint();
        paint2.Color = SKColors.LightGray;
        paint2.IsAntialias = true;

        using var font = new SKFont(SKTypeface.Default, 36);

        font.GetFontMetrics(out var fontMetrics);
        float textHeight = fontMetrics.Descent - fontMetrics.Ascent;

        if (textHeight > 0)
        {
            float ratio = wallThicknessPx * 1.5f / textHeight;
            font.Size = ratio * font.Size;
        }

        canvas.DrawText(Signature, ImageWidth - 2 * wallThicknessPx, imageHeightPx - (int)(2.0 * wallThicknessPx + 0.5), SKTextAlign.Right, font, paint2);

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