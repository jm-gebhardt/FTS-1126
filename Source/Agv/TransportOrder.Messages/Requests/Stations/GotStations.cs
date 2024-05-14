using Gebhardt.Acc.TransportOrder.Messages.MessageContent;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Stations;

/// <summary>
/// Defines a result for the <see cref="GetStations" />.
/// </summary>
/// <param name="Stations">List of stations in map.</param>
public record GotStations(List<Station> Stations);