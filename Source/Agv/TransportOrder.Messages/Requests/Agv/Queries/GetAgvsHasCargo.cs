namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;

/// <summary>
/// Request to get the has cargo state for the specified AgvCorrelationIds.
/// </summary>
/// <param name="CorrelationIds"></param>
public record GetAgvsHasCargo(ICollection<Guid> CorrelationIds);