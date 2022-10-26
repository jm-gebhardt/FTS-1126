using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Common.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles.Resolvers;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles
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
            CreateMap<AgvGraph, GraphDto>()
                .ForMember(nameof(GraphDto.Edges), opt => opt.MapFrom<GraphEdgeValueResolver>())
                // Graph is a record type, therefore we must initialize all properties in the ctor
                .ForCtorParam(nameof(GraphDto.Edges), opt => opt.MapFrom((src, ctx) => new GraphEdgeValueResolver().Resolve(src, null!, null!, ctx)));

            CreateMap<IMap, MapDto>()
                .ForCtorParam(nameof(MapDto.RelatedAgvs), opt => opt.MapFrom((src, ctx) => new RelatedAgvValueResolver().Resolve(src, null!, null!, ctx)));

            CreateMap<IAgv, AgvDto>();
            CreateMap<IOrder, OrderDto>();
            CreateMap<AgvPositionChanged, AgvPositionStatus>()
                .ForCtorParam(nameof(AgvPositionStatus.AgvPosition), opt => opt.MapFrom(src => src.NewAgvPosition));
            CreateMap<AgvPositionReceived, AgvPositionStatus>();
        }
    }
}
