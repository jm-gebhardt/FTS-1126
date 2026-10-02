namespace Gebhardt.Acc.TransportOrder.Messages.Requests.TrafficManagement;

/// <summary>
/// Gets the route for a given AGV
/// </summary>
/// <param name="AgvId">The ID of the AGV</param>
public record GetRoute(Guid AgvId);