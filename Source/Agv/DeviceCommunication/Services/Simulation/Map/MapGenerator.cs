using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using SkiaSharp;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Map
{
    /// <summary>
    /// Generates PNG map for simulation purpose based on a given graph.
    /// <para>
    /// Image width is a fixed value of <see cref="MapGenerator.ImageWidth"/>.
    ///      |---- <see cref="MapGenerator.ImageWidth"/> px ------|
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
    /// The outer area is extended for a fixed value in each direction (see <see cref="MapGenerator.WallDistance"/>).
    /// The resolution therefore is r = totalWidth[m] / ImageWidth
    /// The offset is the distance [m] from the left an upper wall to the origin (0/0).
    /// </para></summary>
    public class MapGenerator
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

        /// <summary>
        /// Initial graph passed by constructor for which the image will be created.
        /// </summary>
        private readonly AgvGraph _graph;


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="graph">Map is generated based on the given graph.</param>
        public MapGenerator(AgvGraph graph)
        {
            _graph = graph;
        }

        /// <summary>
        /// Create a simulation map according to the outer nodes.
        /// </summary>
        /// <returns>Map in PNG format plus additional attributes concerning scaling.</returns>
        /// <exception cref="InvalidOperationException">Graph nodes is empty.</exception>
        public AgvMap Generate()
        {
            if (_graph.Nodes.Length == 0)
            {
                throw new InvalidOperationException("Failed to create simulation map due to empty node list.");
            }

            byte[] map;

            var node = _graph.Nodes[0];
            var min = new SKPoint(node.X, node.Y);
            var max = new SKPoint(node.X, node.Y);

            // Iterate once for better performance.
            for (int i = 1; i < _graph.Nodes.Length; ++i)
            {
                node = _graph.Nodes[i];
                if (node.X < min.X)
                {
                    min.X = node.X;
                }
                if (node.Y < min.Y)
                {
                    min.Y = node.Y;
                }
                if (node.X > max.X)
                {
                    max.X = node.X;
                }
                if (node.Y > max.Y)
                {
                    max.Y = node.Y;
                }
            }

            float resolution = (max.X - min.X + 2 * WallDistance) / ImageWidth;
            float xOffset = -min.X + WallDistance;
            float yOffset = -min.Y + WallDistance;

            int imageHeightPx = (int)Math.Ceiling((max.Y - min.Y + 2 * WallDistance) / resolution);
            float wallThicknessPx = WallThickness / resolution;  // Wall thickness: 24 cm

            var imageInfo = new SKImageInfo(ImageWidth, imageHeightPx);
            using (var surface = SKSurface.Create(imageInfo))
            {
                var canvas = surface.Canvas;
                canvas.Clear(SKColors.White);

                using (var paint = new SKPaint())
                {
                    paint.Color = SKColors.DarkGray;
                    paint.IsAntialias = true;
                    paint.StrokeWidth = wallThicknessPx;
                    paint.Style = SKPaintStyle.Stroke;
                    canvas.DrawRect(paint.StrokeWidth, paint.StrokeWidth, ImageWidth - 2 * paint.StrokeWidth, imageHeightPx - 2 * paint.StrokeWidth, paint);
                }
                var bounds = new SKRect();
                using (var paint = new SKPaint())
                {
                    paint.Color = SKColors.LightGray;
                    paint.TextSize = 36;    // Just an initial value for getting a base to calculate the final font size value.
                    paint.IsAntialias = true;
                    paint.TextAlign = SKTextAlign.Right;
                    paint.MeasureText(Signature, ref bounds);
                    if (bounds.Height > 0)
                    {
                        float ratio = wallThicknessPx * 1.5f / bounds.Height;
                        paint.TextSize = ratio * paint.TextSize;
                    }
                    canvas.DrawText(Signature, ImageWidth - 2 * wallThicknessPx, imageHeightPx - (int)(2.0 * wallThicknessPx + 0.5), paint);
                }

                using (var image = surface.Snapshot())
                {
                    using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                    {
                        map = data.ToArray();
                    }
                }
            }

            return new AgvMap(
                _graph.RelatedMapId,
                _graph.RelatedMapName,
                DateTime.UtcNow,
                Convert.ToBase64String(map),
                resolution, xOffset, yOffset);
        }

        /// <summary>
        /// Try to generate a PNG image from the map data.
        /// </summary>
        /// <param name="agvMap"></param>
        /// <returns></returns>
        [JetBrains.Annotations.PublicAPI]
        public static SKImage GetPng(AgvMap agvMap) => SKImage.FromEncodedData(Convert.FromBase64String(agvMap.ImageData));
    }
}
