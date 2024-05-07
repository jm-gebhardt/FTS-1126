using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.DeviceCommunication.Common.Helpers;
using SkiaSharp;

namespace Gebhardt.Acc.DeviceCommunication.Common.Services;

/// <summary>
/// Reads a PNG image from file and generates a Map.
/// </summary>
public class MapFromFileGenerator : MapGeneratorBase
{
    private string? _filePath;
    private string? _fileName;
    private readonly string _directoryPath;

    /// <inheritdoc cref="MapFromFileGenerator"/>
    /// <param name="directoryPath">The full path to the directory where we can search for the images.</param>
    public MapFromFileGenerator(string directoryPath)
    {
        _directoryPath = directoryPath;
    }

    private string GetFilePath(string mapName)
    {
        string searchPattern = $"{mapName}*.png";

        string[] files = Directory.GetFiles(_directoryPath, searchPattern);
        switch (files.Length)
        {
            case 0:
                throw new FileNotFoundException($"No files found matching pattern {searchPattern}.");

            case > 1:
                throw new InvalidOperationException($"Multiple files found matching pattern {searchPattern}. Expected only one.");
        }

        _filePath = files[0];
        _fileName = Path.GetFileName(_filePath);

        return _filePath;
    }

    private byte[] ReadFileBytes(string mapName)
    {
        string filePath = GetFilePath(mapName);

        if (!CanBeUsed(mapName))
        {
            throw new FileNotFoundException("File not found.", filePath);
        }

        return File.ReadAllBytes(filePath);
    }

    /// <inheritdoc/>
    /// <returns>Yes if all files exist.</returns>
    public override bool CanBeUsed(string mapName)
    {
        string filePath = GetFilePath(mapName);
        bool canBeUsed = File.Exists(filePath);
        return canBeUsed;
    }

    /// <inheritdoc />
    protected override Map GenerateInternal(Guid mapId, string mapName, IGraph graph)
    {
        byte[] imageBytes = ReadFileBytes(mapName);

        using var stream = new SKMemoryStream(imageBytes);
        using var image = SKBitmap.Decode(stream);

        var width = Max - Min;

        float resolutionX = width.X / image.Width;
        float resolutionY = width.Y / image.Height;
        float fullScaleFactor = Math.Max(resolutionX, resolutionY);

        (float scaleUpFactor, float xOffset, float yOffset) = MapHelper.ParseFilename(_fileName!);

        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return new Map(
            mapId,
            mapName,
            DateTime.UtcNow,
            Convert.ToBase64String(data.ToArray()),
            image.Width,
            image.Height,
            fullScaleFactor * scaleUpFactor,
            xOffset,
            yOffset,
            graph.Nodes,
            graph.Edges);
    }
}