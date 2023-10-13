using Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agvs.Queries;

/// <summary>
/// Defines a result for the GetAgvGraphs request.
/// </summary>
public record GotAgvGraphs(List<Graph>? Graphs);