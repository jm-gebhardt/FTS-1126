namespace Gebhardt.Acc.TransportOrder.Messages.Requests.TrafficManagement;

/// <summary>
/// Aborts the route for the given AGV.
/// </summary>
/// <param name="AgvId">The ID of the AGV whose route should be aborted.</param>
public record AbortRoute(Guid AgvId);