using Gebhardt.Acc.TransportOrder.Messages.MessageContent;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Map;

/// <summary>
/// Defines a result for the <see cref="GetMapStations" /> request.
/// </summary>
/// <param name="Stations">List of stations in map.</param>
public record GotMapStations(List<Station> Stations);