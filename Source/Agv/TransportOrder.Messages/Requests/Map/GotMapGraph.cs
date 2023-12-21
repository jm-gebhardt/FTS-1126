using Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Map;

/// <summary>
/// The response which is the graph for the specified map.
/// </summary>
public record GotMapGraph(Graph Graph);