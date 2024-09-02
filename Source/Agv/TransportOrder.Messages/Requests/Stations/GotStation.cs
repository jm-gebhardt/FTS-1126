using Gebhardt.Acc.TransportOrder.Domain.Models.Station;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Stations;

/// <summary>
/// Defines a result for the <see cref="GetStation" /> request.
/// </summary>
/// <param name="Station">The returned station.</param>
public record GotStation(Station Station);