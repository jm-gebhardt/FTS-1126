using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Domain.Models.Graphs;
using Graph = Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent.Graph;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Profiles;

/// <summary>
/// Profile for AgvGraph
/// </summary>
public class AgvGraphProfile : Profile
{
    /// <inheritdoc />
    public AgvGraphProfile()
    {
        CreateMap<GraphEdge, MessageContent.GraphEdge>(MemberList.None);
        CreateMap<GraphNode, MessageContent.GraphNode>();
        CreateMap<AgvGraph, Graph>()
            .ForMember(nameof(Graph.Edges), opt => opt.MapFrom<AgvGraphGraphEdgeResolver>())
            // Graph is a record type, therefore we must initialize all properties in the ctor
            .ForCtorParam(nameof(Graph.Edges), opt => opt.MapFrom((src, ctx) => new AgvGraphGraphEdgeResolver().Resolve(src, null!, null!, ctx)));
    }
}