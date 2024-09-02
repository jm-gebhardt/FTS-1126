using Gebhardt.Acc.TransportOrder.Domain.Models.Station;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Map;

/// <summary>
/// Defines a result for the <see cref="GetMapStations" /> request.
/// </summary>
/// <param name="Stations">Collection of stations in map.</param>
public record GotMapStations(ICollection<Station> Stations);