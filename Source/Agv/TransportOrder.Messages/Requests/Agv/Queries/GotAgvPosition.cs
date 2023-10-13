using Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;

/// <summary>
/// Defines a result for the <see cref="GetAgvPosition"/> request.
/// </summary>
public record GotAgvPosition(AgvPositionStatus? AgvPositions);

