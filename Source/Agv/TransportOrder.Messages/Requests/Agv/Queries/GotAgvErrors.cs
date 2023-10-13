namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;

/// <summary>
/// Defines a result for GetAgvErrorHistory request
/// </summary>
/// <param name="AgvErrors"></param>
public record GotAgvErrors(List<AgvError> AgvErrors);