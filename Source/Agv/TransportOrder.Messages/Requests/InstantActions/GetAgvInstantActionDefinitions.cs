namespace Gebhardt.Acc.TransportOrder.Messages.Requests.InstantActions;

/// <summary>
/// Provides a request to get all instant action definitions for an agv.
/// </summary>
/// <param name="AgvCorrelationId">The agv correlation id.</param>
public record GetAgvInstantActionDefinitions(Guid AgvCorrelationId);