namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Map;

/// <summary>
/// The response which is the graph for the specified map.
/// </summary>
public record GotMapGraph(MessageContent.Graph Graph);