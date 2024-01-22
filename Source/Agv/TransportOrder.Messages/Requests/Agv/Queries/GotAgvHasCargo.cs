using Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;

/// <summary>
/// Response to get the has agv cargo
/// </summary>
/// <param name="AgvHasCargo">AgvHasCargo</param>
public record GotAgvHasCargo(AgvHasCargo AgvHasCargo);