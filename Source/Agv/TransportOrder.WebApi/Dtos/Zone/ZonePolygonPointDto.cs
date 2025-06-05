using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.Zone;

/// <summary>
/// Represents a polygon point of a zone.
/// </summary>
/// <param name="X">The x coordinate of the polygon point.</param>
/// <param name="Y">The y coordinate of the polygon point.</param>
/// <param name="Index">The index of the polygon point.</param>
/// <param name="LocationId">The unique id of the polygon point.</param>
[PublicAPI]
public record ZonePolygonPointDto(double X, double Y, int Index, Guid LocationId);