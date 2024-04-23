using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using JetBrains.Annotations;
using Graph = Gebhardt.Acc.TransportOrder.Messages.MessageContent.Graph;

namespace Gebhardt.Acc.TransportOrder.Messages.Profiles;

/// <summary>
/// The profile for mapping between <see cref="AgvGraph"/> and <see cref="EfAgvGraph"/>
/// </summary>
[UsedImplicitly]
public class GraphProfile : Profile
{
    /// <inheritdoc cref="GraphProfile"/>
    public GraphProfile()
    {
        CreateMap<AgvGraph, EfAgvGraph>().ReverseMap()
            ;

        CreateMap<GraphEdge, EfEdge>()
            .ForMember(n => n.Maps, opt => opt.Ignore())
            .ForMember(n => n.AgvGraphs, opt => opt.Ignore())
            .ReverseMap()
            ;

        CreateMap<GraphNode, EfNode>()
            .ForMember(n => n.Maps, opt => opt.Ignore())
            .ForMember(n => n.AgvGraphs, opt => opt.Ignore())
            .ReverseMap()
            ;

        CreateMap<GraphNode, MessageContent.GraphNode>();

        CreateMap<IGraph, Graph>()
            .ForMember(n => n.CorrelationId, opt => opt.Ignore())
            .ForMember(nameof(Graph.Edges), opt => opt.MapFrom<GraphGraphEdgeResolver>())
            .ReverseMap()
            ;
    }
}