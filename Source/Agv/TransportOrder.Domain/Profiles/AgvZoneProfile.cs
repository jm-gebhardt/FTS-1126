using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.TransportOrder.Domain.Models.Zone;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Profiles;

/// <summary>
/// Profile for an agv zone.
/// </summary>
public class AgvZoneProfile : Profile
{
    /// <inheritdoc />
    public AgvZoneProfile()
    {
        CreateMap<AgvZone, Zone>().ConvertUsing<AgvZoneConverter>();
    }
}

/// <summary>
/// Provides a type converter for <see cref="AgvZone"/> to <see cref="Zone"/>
/// </summary>
[UsedImplicitly]
public class AgvZoneConverter : ITypeConverter<AgvZone, Zone>
{
    /// <inheritdoc />
    public Zone Convert(AgvZone source, Zone destination, ResolutionContext context)
    {
        var correlationId = source.ZoneId.ToGuid();
        var polygon = source.Polygon.Select((p, index) => new ZonePolygonPoint(p.X, p.Y, index, correlationId)).ToList();
        return new Zone(polygon, source.ZoneId, correlationId, source.Capacity);
    }
}