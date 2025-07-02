using JetBrains.Annotations;

namespace Gebhardt.Acc.Statistics.WebApi.Dtos;

/// <summary>
/// Represents a data transfer object for a scalar heatmap.
/// </summary>
public class ScalarHeatmapDto
{
    /// <summary>
    /// The grid id.
    /// </summary>
    [PublicAPI]
    public Guid GridId { get; init; }

    /// <summary>
    /// The correlationId of the map.
    /// </summary>
    [PublicAPI]
    public Guid? MapId { get; init; }

    /// <summary>
    /// Heatmap data represented as a collection of triplets: [xIndex, yIndex, value].  
    /// Each entry defines the position of a grid cell and its associated scalar metric  
    /// (e.g., signal strength in dBm, count).
    /// </summary>
    [PublicAPI]
    public ICollection<ICollection<float>> Values { get; init; } = null!;

    /// <inheritdoc cref="ScalarHeatmapDto"/>>
    public ScalarHeatmapDto(Guid gridId, Guid? mapId, ICollection<ICollection<float>> values)
    {
        GridId = gridId;
        MapId = mapId;
        Values = values;
    }

    /// <summary>
    /// AutoMapper
    /// </summary>
    private ScalarHeatmapDto()
    {
    }
}