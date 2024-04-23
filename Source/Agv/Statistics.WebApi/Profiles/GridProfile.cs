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
        CreateMap<Grid, GridDto>();
    }
}