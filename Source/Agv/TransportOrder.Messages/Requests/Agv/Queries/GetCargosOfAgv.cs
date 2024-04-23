using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;

/// <summary>
/// Request to get the Cargos for the specified AgvCorrelationId
/// </summary>
/// <param name="CorrelationId">The correlationId of the Agv</param>
public record GetCargosOfAgv(Guid CorrelationId) : CorrelatedBy<Guid>;