using AutoMapper;
using JetBrains.Annotations;
using Statistics.Domain.ReadModels;
using Statistics.WebApi.Dtos;

namespace Statistics.WebApi.Profiles;

/// <summary>
/// Profile for mapping the <see cref="PositionFrequencyDto"/>.
/// </summary>
[UsedImplicitly]
public class PositionFrequencyProfile : Profile
{
    /// <inheritdoc />
    public PositionFrequencyProfile()
    {
        CreateMap<PositionFrequency, PositionFrequencyDto>();
    }
}