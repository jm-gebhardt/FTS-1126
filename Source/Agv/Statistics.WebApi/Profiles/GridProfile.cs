using AutoMapper;
using JetBrains.Annotations;
using Statistics.Domain.Models;
using Statistics.WebApi.Dtos;

namespace Statistics.WebApi.Profiles;

/// <summary>
/// Profile for mapping the <see cref="GridDto"/>.
/// </summary>
[UsedImplicitly]
public class GridProfile : Profile
{
    /// <inheritdoc />
    public GridProfile()
    {
        CreateMap<Grid, GridDto>();
    }
}