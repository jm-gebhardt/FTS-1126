using Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agvs.Queries;

/// <summary>
/// Defines a result for the GetAgvPositions request
/// </summary>
public record GotAgvPositions(List<AgvPositionStatus>? AgvPositions);