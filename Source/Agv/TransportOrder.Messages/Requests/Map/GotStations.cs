using Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Map;

/// <summary>
/// Defines a result for the <see cref="GetStations" />.
/// </summary>
/// <param name="Stations">List of stations in map.</param>
public record GotStations(List<Station> Stations);