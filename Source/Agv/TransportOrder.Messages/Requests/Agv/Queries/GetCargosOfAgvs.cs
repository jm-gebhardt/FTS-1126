namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;

/// <summary>
/// Request to get the Cargos for the specified AgvCorrelationIds
/// </summary>
/// <param name="CorrelationIds">The correlationIds of the requested Agv</param>
public record GetCargosOfAgvs(ICollection<Guid> CorrelationIds);