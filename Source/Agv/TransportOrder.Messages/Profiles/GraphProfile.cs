using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using JetBrains.Annotations;
using TransportOrder.Domain.Models.Graph;

namespace TransportOrder.Messages.Profiles;

/// <summary>
/// The profile for mapping between <see cref="AgvGraph"/> and <see cref="EfGraph"/>
/// </summary>
[UsedImplicitly]
public class GraphProfile : Profile
{
    /// <summary>
    /// Constructor
    /// </summary>
    public GraphProfile()
    {
        CreateMap<AgvGraph, EfGraph>()
            .ReverseMap();

        CreateMap<GraphEdge, EfEdge>()
            .ForMember(n => n.GraphCorrelationId, opt => opt.Ignore())
            .ReverseMap();

        CreateMap<GraphNode, EfNode>()
            .ForMember(n => n.GraphCorrelationId, opt => opt.Ignore())
            .ReverseMap();
    }
}