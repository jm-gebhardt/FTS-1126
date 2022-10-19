using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Profiles.Resolvers;
using Gebhardt.StoreWare.Agv.TransportOrder.Web.Dtos;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Profiles
{
    /// <summary>
    /// The AutoMapper profile for the Web Dtos
    /// </summary>
    public class MappingProfile : Profile
    {
        /// <summary>
        /// The constant name for the AgvMapRelation mapping
        /// </summary>
        public const string AgvMapRelationsName = "agvMapRelations";

        /// <summary>
        /// Creates the maps 
        /// </summary>
        public MappingProfile()
        {
            CreateMap<AgvGraph, Graph>()
                .ForMember(nameof(Graph.Edges), opt => opt.MapFrom<GraphEdgeValueResolver>())
                // Graph is a record type, therefore we must initialize all properties in the ctor
                .ForCtorParam(nameof(Graph.Edges), opt => opt.MapFrom((src, ctx) => new GraphEdgeValueResolver().Resolve(src, null!, null!, ctx)));

            CreateMap<Persistence.Models.Map, Map>()
                .ForCtorParam(nameof(Map.RelatedAgvs), opt => opt.MapFrom((src, ctx) => new RelatedAgvValueResolver().Resolve(src, null!, null!, ctx)));

            CreateMap<IAgv, Web.Dtos.Agv>();
            CreateMap<IOrder, Order>();
            CreateMap<AgvPositionChanged, AgvPositionStatus>()
                .ForCtorParam(nameof(AgvPositionStatus.AgvPosition), opt => opt.MapFrom(src => src.NewAgvPosition));
            CreateMap<AgvPositionReceived, AgvPositionStatus>();
        }
    }
}
