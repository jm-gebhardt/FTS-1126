namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agvs.Queries;

/// <summary>
/// Provides a request to get error history of the selected agvs.
/// </summary>
/// <param name="CorrelationIds">Correlation ids of selected agvs.</param>
public record GetErrorHistoryForAgvs(ICollection<Guid> CorrelationIds, DateTime DateFrom, DateTime DateTo);