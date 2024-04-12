using Gebhardt.StoreWare.Agv.Statistics.Domain.ReadModels;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.Statistics.WebApi.Dtos;

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
    /// The CorrelationId of the Map..
    /// </summary>
    [PublicAPI]
    public Guid? MapId { get; init; }

    /// <summary>
    /// The position frequencies.
    /// </summary>
    [PublicAPI]
    public ICollection<ICollection<float>> Frequencies { get; init; } = null!;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="gridId">The CorrelationId of the Grid.</param>
    /// <param name="mapId">The CorrelationId of the Map.</param>
    /// <param name="frequencies">The position frequencies [[xIndex, yIndex, value]].</param>
    public PositionFrequencyDto(Guid gridId, Guid? mapId, ICollection<ICollection<float>> frequencies)
    {
        GridId = gridId;
        MapId = mapId;
        Frequencies = frequencies;
    }

    /// <summary>
    /// AutoMapper
    /// </summary>
    private PositionFrequencyDto()
    {
    }
}