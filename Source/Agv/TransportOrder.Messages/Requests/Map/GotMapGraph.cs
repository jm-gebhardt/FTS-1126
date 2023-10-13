using Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Map;

/// <summary>
/// Defines the result of a <see cref="GetMapGraph"/> request.
/// </summary>
public record GotMapGraph(Graph Graph);