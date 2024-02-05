using AutoMapper;
using Gebhardt.StoreWare.Agv.Statistics.Domain.ReadModels;
using Gebhardt.StoreWare.Agv.Statistics.WebApi.Dtos;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.Statistics.WebApi.Profiles;

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