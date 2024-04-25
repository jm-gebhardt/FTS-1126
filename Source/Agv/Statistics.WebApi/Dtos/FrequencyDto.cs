using Gebhardt.Acc.Statistics.Domain.ReadModels;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Statistics.WebApi.Dtos;

/// <summary>
/// Provides a Dto for <see cref="CountHeatmap"/>
/// </summary>
public class FrequencyDto
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
    /// The frequencies [[xIndex, yIndex, count], ..].
    /// </summary>
    [PublicAPI]
    public ICollection<ICollection<float>> Frequencies { get; init; } = null!;

    /// <inheritdoc cref="FrequencyDto"/>>
    public FrequencyDto(Guid gridId, Guid? mapId, ICollection<ICollection<float>> frequencies)
    {
        GridId = gridId;
        MapId = mapId;
        Frequencies = frequencies;
    }

    /// <summary>
    /// AutoMapper
    /// </summary>
    private FrequencyDto()
    {
    }
}