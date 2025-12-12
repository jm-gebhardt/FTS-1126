using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Extensions;
using JetBrains.Annotations;
using Graph = Gebhardt.Acc.TransportOrder.Messages.MessageContent.Graph;

namespace Gebhardt.Acc.TransportOrder.Messages.Profiles;

/// <summary>
/// The profile for mapping of <see cref="AgvGraph"/>.
/// </summary>
[UsedImplicitly]
public class GraphProfile : Profile
{
    /// <inheritdoc cref="GraphProfile"/>
    public GraphProfile()
    {
        CreateMap<GraphNode, MessageContent.GraphNode>()
            .ForMember(n => n.Type, opt => opt.MapFrom(gn => gn.GetPrimaryType()));
        CreateMap<IGraph, Graph>()
            .ForMember(n => n.CorrelationId, opt => opt.Ignore())
            .ForMember(nameof(Graph.Edges), opt => opt.MapFrom<GraphGraphEdgeResolver>())
            .ReverseMap()
            ;
    }
}