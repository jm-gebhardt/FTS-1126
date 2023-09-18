namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.ActionGroup;

/// <summary>
/// Provides a request to get an action group.
/// </summary>
/// <param name="CorrelationId">The correlation id.</param>
public record GetActionGroup(Guid CorrelationId);