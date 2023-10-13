using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent;
using GraphEdge = Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.GraphEdge;
using GraphNode = Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.GraphNode;

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
            .ForMember(nameof(Graph.Edges), opt => opt.MapFrom<GraphEdgeValueResolver>())
            // Graph is a record type, therefore we must initialize all properties in the ctor
            .ForCtorParam(nameof(Graph.Edges), opt => opt.MapFrom((src, ctx) => new GraphEdgeValueResolver().Resolve(src, null!, null!, ctx)));
    }
}