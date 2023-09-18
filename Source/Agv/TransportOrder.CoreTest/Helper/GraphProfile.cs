using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Graph;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;

/// <summary>
/// The profile for mapping between <see cref="AgvGraph"/> and <see cref="EfGraph"/>
/// </summary>
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