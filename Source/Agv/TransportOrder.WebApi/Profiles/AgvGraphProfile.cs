using AutoMapper;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Graph;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles.Resolver;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles;

/// <summary>
/// Profile for AgvGraph related dtos.
/// </summary>
public class AgvGraphProfile : Profile
{
    /// <inheritdoc />
    public AgvGraphProfile()
    {
        CreateMap<GraphEdge, GraphEdgeDto>(MemberList.None);
        CreateMap<GraphNode, GraphNodeDto>();
        CreateMap<Graph, GraphDto>()
            // Graph is a record type, therefore we must initialize all properties in the ctor
            .ForCtorParam(nameof(GraphDto.Edges), opt =>
                opt.MapFrom((src, ctx) => new GraphEdgeValueResolver().Resolve(src, null!, null!, ctx)));
    }
}