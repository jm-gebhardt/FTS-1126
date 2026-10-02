using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.TrafficManagement;

/// <summary>
/// Defines a result for the &lt;see cref="GetRoute"/&gt;
/// </summary>
/// <param name="AgvId">The ID of the AGV for which the route information is returned.</param>
/// <param name="CurrentRoutingNode">The current routing node of the AGV.</param>
/// <param name="RouteStepInfos">The list of route step descriptions for the AGV.</param>
[PublicAPI]
public record GotRoute(Guid AgvId, string CurrentRoutingNode, List<string> RouteStepInfos);