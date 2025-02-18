namespace Gebhardt.Acc.TransportOrder.Messages.Requests.InstantActions;

/// <summary>
/// Provides a request to get all instant action definitions for the given agvs.
/// </summary>
/// <param name="AgvCorrelationIds">The correlation ids of the agvs.</param>
public record GetAgvsInstantActionsDefinitions(ICollection<Guid> AgvCorrelationIds);