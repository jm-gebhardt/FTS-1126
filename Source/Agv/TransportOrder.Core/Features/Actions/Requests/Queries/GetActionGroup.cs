namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Requests.Queries;

/// <summary>
/// Provides a request to get an action group.
/// </summary>
/// <param name="CorrelationId">The correlation id.</param>
public record GetActionGroup(Guid CorrelationId);