namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;

/// <summary>
/// Request to get the AgvRoute for the specified AgvCorrelationIds
/// </summary>
/// <param name="CorrelationIds">The CorrelationId of the Agv</param>
public record GetCurrentAgvRoutes(ICollection<Guid> CorrelationIds);