using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Extensions;
using Graph = Gebhardt.Acc.TransportOrder.Messages.MessageContent.Graph;

namespace Gebhardt.Acc.TransportOrder.Messages.Profiles;

/// <summary>
/// Profile for AgvGraph
/// </summary>
public class AgvGraphProfile : Profile
{
    /// <inheritdoc />
    public AgvGraphProfile()
    {
        CreateMap<GraphEdge, MessageContent.GraphEdge>(MemberList.None);
        CreateMap<GraphNode, MessageContent.GraphNode>()
            .ForMember(n => n.Type, opt => opt.MapFrom(gn => gn.GetPrimaryType()));
        CreateMap<AgvGraph, Graph>()
            .ForMember(nameof(Graph.Edges), opt => opt.MapFrom<AgvGraphGraphEdgeResolver>())
            // Graph is a record type, therefore we must initialize all properties in the ctor
            .ForCtorParam(nameof(Graph.Edges), opt => opt.MapFrom((src, ctx) => new AgvGraphGraphEdgeResolver().Resolve(src, null!, null!, ctx)));
    }
}