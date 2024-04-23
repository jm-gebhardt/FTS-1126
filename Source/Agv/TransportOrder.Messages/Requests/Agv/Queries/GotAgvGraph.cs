using Gebhardt.Acc.TransportOrder.Messages.MessageContent;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;

/// <summary>
/// Defines a result for the GetAgvGraph request
/// </summary>
public record GotAgvGraph(Graph AgvGraph);