namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Agvs.Queries;

/// <summary>
/// Provides a request to get active errors of the selected agvs.
/// </summary>
/// <param name="CorrelationIds">Correlation ids of selected agvs.</param>
public record GetActiveErrorsForAgvs(ICollection<Guid> CorrelationIds);