using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace Gebhardt.Acc.Common.Domain.Services;

/// <summary>
/// Composites two source maps onto a shared canvas.
/// </summary>
public static class MapCompositor
{
    /// <summary>
    /// Renders <paramref name="baseMap"/> and then <paramref name="overlayMap"/> onto a white canvas
    /// sized to <paramref name="canvas"/>, returning the result as a PNG byte array.
    /// </summary>
    public static byte[] Composite(Map baseMap, Map overlayMap, Map canvas, ILogger logger)
    {
        using var bitmap = new SKBitmap(canvas.Width, canvas.Height);
        using var skCanvas = new SKCanvas(bitmap);
        skCanvas.Clear(SKColors.White);

        DrawMapOntoCanvas(skCanvas, baseMap, canvas, logger);
        DrawMapOntoCanvas(skCanvas, overlayMap, canvas, logger);

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static void DrawMapOntoCanvas(SKCanvas canvas, Map sourceMap, Map extendedMap, ILogger logger)
    {
        byte[] bytes = Convert.FromBase64String(sourceMap.ImageData);
        using var sourceBitmap = SKBitmap.Decode(bytes);
        if (sourceBitmap is null)
        {
            logger.LogWarning("Could not decode image for map {MapId}", sourceMap.MapId);
            return;
        }

        float scale = sourceMap.Resolution / extendedMap.Resolution;

        // X: both axes increase in the same direction → simple offset difference.
        float destX = (sourceMap.XOffset - extendedMap.XOffset) / extendedMap.Resolution;

        // Y: right-hand coordinate system has world Y increasing upward, but SkiaSharp row 0 is
        // the TOP of the image (highest world Y). The canvas top row corresponds to
        // canvas.YOffset + canvas.Height * canvas.Resolution (= canvas maxWorldY), so the source
        // image's top (its maxWorldY = source.YOffset + source.Height * source.Resolution) maps to:
        //   destBottom = canvas.Height + (canvas.YOffset - source.YOffset) / canvas.Resolution
        float destBottom = extendedMap.Height + (extendedMap.YOffset - sourceMap.YOffset) / extendedMap.Resolution;
        float destY = destBottom - sourceMap.Height * scale;

        var destRect = new SKRect(
            destX,
            destY,
            destX + sourceMap.Width * scale,
            destBottom);

        logger.LogDebug("Drawing map {MapId} to canvas region ({Left}/{Top}/{Right}/{Bottom})",
            sourceMap.MapId, destRect.Left, destRect.Top, destRect.Right, destRect.Bottom);

        canvas.DrawBitmap(sourceBitmap, destRect);
    }
}
