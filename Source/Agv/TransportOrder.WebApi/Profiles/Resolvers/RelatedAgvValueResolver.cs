using System.Collections.Immutable;
using AutoMapper;
using Gebhardt.StoreWare.Agv.TransportOrder.Common.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles.Resolvers
{
    /// <summary>
    /// The custom value resolver, maps from Persistence model of Map and the destination Dto Map to the RelatedAgvs property
    /// </summary>
    public class RelatedAgvValueResolver : IValueResolver<IMap, MapDto, object>
    {
        /// <summary>
        /// The resolve method for mapping the RelatedAgvs coming from the ResolutionContext.Items which has a value of Dictionary 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="destMember"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public object Resolve(IMap source, MapDto destination, object destMember, ResolutionContext context)
        {
            object agvMapRelations = context.Items[MappingProfile.AgvMapRelationsName];
            if (agvMapRelations is ImmutableDictionary<Guid, Guid[]> relations)
            {
                return relations.GetValueOrDefault(source.CorrelationId) ?? Array.Empty<Guid>();
            }

            return Array.Empty<Guid>();
        }
    }
}
