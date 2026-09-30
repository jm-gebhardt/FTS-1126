using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.AgvType;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Profiles;

/// <summary>
/// Mapping for AGV types.
/// </summary>
[UsedImplicitly]
public class AgvTypeProfile : Profile
{
    /// <inheritdoc/>
    public AgvTypeProfile()
    {
        CreateMap<AgvTypeReadModel, AgvTypeDto>()
            .ForMember(d => d.Factsheet, o => o.MapFrom(r => r.RawData))
            .ForMember(d => d.HasFactsheet, o => o.Ignore());
        CreateMap<AgvTypeInfo, AgvTypeDto>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Factsheet, o => o.MapFrom(i => i.RawData))
            .ForMember(d => d.HasFactsheet, o => o.Ignore())
            .ForMember(d => d.HasGraph, o => o.Ignore());
    }
}