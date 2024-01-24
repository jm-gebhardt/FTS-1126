using JetBrains.Annotations;
using Statistics.Domain.ReadModels;

namespace Statistics.WebApi.Dtos;

/// <summary>
/// Provides a Dto for <see cref="PositionFrequency"/>
/// </summary>
public class PositionFrequencyDto
{
    /// <summary>
    /// The grid id.
    /// </summary>
    [PublicAPI]
    public Guid GridId { get; init; }

    /// <summary>
    /// The position frequencies.
    /// </summary>
    [PublicAPI]
    public ICollection<ICollection<float>> Frequencies { get; init; } = null!;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="gridId"></param>
    /// <param name="frequencies"></param>
    public PositionFrequencyDto(Guid gridId, ICollection<ICollection<float>> frequencies)
    {
        GridId = gridId;
        Frequencies = frequencies;
    }

    /// <summary>
    /// AutoMapper
    /// </summary>
    private PositionFrequencyDto()
    {
    }
}