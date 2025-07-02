using AutoMapper;
using Gebhardt.Acc.Statistics.Domain.ReadModels;
using Gebhardt.Acc.Statistics.WebApi.Dtos;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Statistics.WebApi.Profiles;

/// <summary>
/// Profile for mapping the <see cref="ScalarHeatmapDto"/>.
/// </summary>
[UsedImplicitly]
public class HeatmapProfile : Profile
{
    /// <inheritdoc />
    public HeatmapProfile()
    {
        CreateMap<ScalarHeatmap, ScalarHeatmapDto>();
    }
}