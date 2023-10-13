using Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;

/// <summary>
/// Defines a result for the GetAgvGraph request
/// </summary>
public record GotAgvGraph(Graph AgvGraph);