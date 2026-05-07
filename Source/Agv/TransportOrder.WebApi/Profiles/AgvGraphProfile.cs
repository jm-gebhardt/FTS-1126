using AutoMapper;
using Gebhardt.Acc.TransportOrder.Messages.MessageContent;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Graph;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles.Resolver;

namespace Gebhardt.Acc.TransportOrder.WebApi.Profiles;

/// <summary>
/// Profile for AgvGraph related dtos.
/// </summary>
public class AgvGraphProfile : WebApiProfile
{
    /// <inheritdoc />
    public AgvGraphProfile()
    {
        CreateMap<GraphEdge, GraphEdgeDto>(MemberList.None)
            .ForMember(dest => dest.FromNodeId, opt => opt.MapFrom(src => src.From.Name))
            .ForMember(dest => dest.ToNodeId, opt => opt.MapFrom(src => src.To.Name));
        CreateMap<GraphNode, GraphNodeDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Type, opt => opt.MapFrom(source => source.Type.First()));
        CreateMap<Graph, GraphDto>()
            // Graph is a record type, therefore we must initialize all properties in the ctor
            .ForCtorParam(nameof(GraphDto.Edges), opt =>
                opt.MapFrom((src, ctx) => new GraphEdgeValueResolver().Resolve(src, null!, null!, ctx)));
    }
}