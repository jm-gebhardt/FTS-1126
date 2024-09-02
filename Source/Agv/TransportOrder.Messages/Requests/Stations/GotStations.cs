
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Stations;

/// <summary>
/// Defines a result for the <see cref="GetStations" />.
/// </summary>
/// <param name="Stations">Collection of stations in map.</param>
public record GotStations(ICollection<Station> Stations);