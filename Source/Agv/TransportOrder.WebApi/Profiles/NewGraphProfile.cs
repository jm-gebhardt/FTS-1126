using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models.NewGraph;
using Gebhardt.Acc.TransportOrder.Domain.Models.AgvTypes;
using Gebhardt.Acc.TransportOrder.Domain.Models.Views;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.NewGraph;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles.Converter;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Profiles;

/// <summary>
/// Maps the graph and the views to their DTOs.
/// </summary>
[UsedImplicitly]
public class NewGraphProfile : Profile
{
    /// <inheritdoc cref="NewGraphProfile"/>
    public NewGraphProfile()
    {
        CreateMap<(Graph Graph, IReadOnlyList<LoadState> LoadStates), GraphDto>().ConvertUsing<GraphDtoConverter>();

        CreateMap<ViewArea, ViewAreaDto>().ReverseMap();
        CreateMap<ViewBackground, ViewBackgroundDto>();
        CreateMap<View, ViewDto>();
    }
}