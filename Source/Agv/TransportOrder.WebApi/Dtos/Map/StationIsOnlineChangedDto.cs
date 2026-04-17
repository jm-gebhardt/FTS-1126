using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.Map;

/// <summary>
/// The dto for station online state changes.
/// </summary>
/// <param name="StationId">The id of the station.</param>
/// <param name="IsOnline">True if the station's connection is healthy.</param>
[PublicAPI]
public record StationIsOnlineChangedDto(Guid StationId, bool IsOnline);