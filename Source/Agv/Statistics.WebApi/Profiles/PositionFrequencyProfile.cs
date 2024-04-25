using AutoMapper;
using Gebhardt.Acc.Statistics.Domain.ReadModels;
using Gebhardt.Acc.Statistics.WebApi.Dtos;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Statistics.WebApi.Profiles;

/// <summary>
/// Profile for mapping the <see cref="FrequencyDto"/>.
/// </summary>
[UsedImplicitly]
public class PositionFrequencyProfile : Profile
{
    /// <inheritdoc />
    public PositionFrequencyProfile()
    {
        CreateMap<CountHeatmap, FrequencyDto>();
    }
}