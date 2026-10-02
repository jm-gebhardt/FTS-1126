namespace Gebhardt.Acc.TransportOrder.Messages.Requests.TrafficManagement;

/// <summary>
/// Starts a new route for the given AGV.
/// </summary>
/// <param name="AgvId">The ID of the AGV</param>
/// <param name="NodeShortName">The short name of the target node</param>
public record StartRouteToNode(Guid AgvId, string NodeShortName);