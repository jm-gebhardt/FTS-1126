using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using TransportOrder.Messages.Requests.Agv;
using GraphEdge = TransportOrder.Messages.Requests.Agv.GraphEdge;
using GraphNode = TransportOrder.Messages.Requests.Agv.GraphNode;

namespace TransportOrder.Messages.Profiles;

/// <summary>
/// Profile for AgvGraph
/// </summary>
public class AgvGraphProfile : Profile
{
    /// <inheritdoc />
    public AgvGraphProfile()
    {
        CreateMap<Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.GraphEdge, GraphEdge>(MemberList.None);
        CreateMap<Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.GraphNode, GraphNode>();
        CreateMap<AgvGraph, Graph>()
            .ForMember(nameof(Graph.Edges), opt => opt.MapFrom<GraphEdgeValueResolver>())
            // Graph is a record type, therefore we must initialize all properties in the ctor
            .ForCtorParam(nameof(Graph.Edges), opt => opt.MapFrom((src, ctx) => new GraphEdgeValueResolver().Resolve(src, null!, null!, ctx)));
    }
}