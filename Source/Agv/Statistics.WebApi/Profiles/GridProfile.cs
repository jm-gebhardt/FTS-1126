using AutoMapper;
using Gebhardt.StoreWare.Agv.Statistics.Domain.Models;
using Gebhardt.StoreWare.Agv.Statistics.WebApi.Dtos;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.Statistics.WebApi.Profiles;

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