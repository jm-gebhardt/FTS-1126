using AutoMapper;
using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.WebApi.Dtos;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Statistics.WebApi.Profiles;

/// <summary>
/// Profile for mapping the <see cref="GridDto"/>.
/// </summary>
[UsedImplicitly]
public class GridProfile : Profile
{
    /// <inheritdoc />
    public GridProfile()
    {
        CreateMap<Grid, GridDto>()
            .ForMember(dest => dest.XStart, opt => opt.MapFrom(src => (float) src.XStep))
            .ForMember(dest => dest.YStart, opt => opt.MapFrom(src => (float) src.YStep))
            .ForMember(dest => dest.XSize, opt => { opt.MapFrom(src => src.Maps!.Single().XMax - src.Maps!.Single().XMin); })
            .ForMember(dest => dest.YSize, opt => { opt.MapFrom(src => src.Maps!.Single().YMax - src.Maps!.Single().YMin); })
            ;
    }
}