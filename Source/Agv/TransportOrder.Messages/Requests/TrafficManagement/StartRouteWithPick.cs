namespace Gebhardt.Acc.TransportOrder.Messages.Requests.TrafficManagement;

/// <summary>
/// Starts a new route with pick at the target node for the given AGV.
/// </summary>
/// <param name="AgvId">The ID of the AGV</param>
/// <param name="NodeShortName">The short name of the target node</param>
public record StartRouteWithPick(Guid AgvId, string NodeShortName);