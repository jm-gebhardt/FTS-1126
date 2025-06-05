namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.Zone;

/// <summary>
/// Represents a zone.
/// </summary>
/// <param name="Polygon">The polygon of the zone.</param>
/// <param name="ZoneId">The string id of the zone.</param>
/// <param name="CorrelationId">The internal unique id of the agv.</param>
/// <param name="Capacity">The capacity of the agv.</param>
public record ZoneDto(ICollection<ZonePolygonPointDto> Polygon, string ZoneId, Guid CorrelationId, int Capacity);