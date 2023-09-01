using System.Collections.Immutable;
using AutoMapper;
using TransportOrder.Domain.Models;
using TransportOrder.Messages.Requests.Map;

namespace TransportOrder.Messages.Profiles;

/// <summary>
/// The custom value resolver, maps from Persistence model of Map and the destination Dto Map to the RelatedAgvs property
/// </summary>
public class RelatedMapAgvValueResolver : IValueResolver<IMap, Map, object>
{
    /// <summary>
    /// The constant name for the AgvMapRelation mapping
    /// </summary>
    public const string AgvMapRelationsName = "agvMapRelations";
        
    /// <summary>
    /// The resolve method for mapping the RelatedAgvs coming from the ResolutionContext.Items which has a value of Dictionary 
    /// </summary>
    /// <param name="source"></param>
    /// <param name="destination"></param>
    /// <param name="destMember"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public object Resolve(IMap source, Map destination, object destMember, ResolutionContext context)
    {
        object agvMapRelations = context.Items[AgvMapRelationsName];
        if (agvMapRelations is ImmutableDictionary<Guid, Guid[]> relations)
        {
            return relations.GetValueOrDefault(source.CorrelationId) ?? Array.Empty<Guid>();
        }

        return Array.Empty<Guid>();
    }
}