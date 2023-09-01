using AutoMapper;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Graph;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles.Resolver;
using TransportOrder.Messages.Requests.Agv;
using GraphEdge = TransportOrder.Messages.Requests.Agv.GraphEdge;
using GraphNode = TransportOrder.Messages.Requests.Agv.GraphNode;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles;

/// <summary>
/// Profile for AgvGraph
/// </summary>
public class AgvGraphProfile : Profile
{
    /// <inheritdoc />
    public AgvGraphProfile()
    {
        CreateMap<GraphEdge, GraphEdgeDto>(MemberList.None);
        CreateMap<GraphNode, GraphNodeDto>();
        CreateMap<Graph, GraphDto>()
            .ForMember(nameof(GraphDto.Edges), opt => opt.MapFrom<GraphEdgeValueResolver>())
            // Graph is a record type, therefore we must initialize all properties in the ctor
            .ForCtorParam(nameof(GraphDto.Edges), opt => 
                opt.MapFrom((src, ctx) => new GraphEdgeValueResolver().Resolve(src, null!, null!, ctx)));
    }
}