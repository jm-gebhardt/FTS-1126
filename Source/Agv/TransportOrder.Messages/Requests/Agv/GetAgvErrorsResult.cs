namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv;

/// <summary>
/// Defines a result for GetAgvErrorHistory request
/// </summary>
/// <param name="AgvErrors"></param>
public record GetAgvErrorsResult(List<AgvError> AgvErrors);