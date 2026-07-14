using Gebhardt.Acc.Common.Messages.Route.AgvActions;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Messages.Route.RouteSteps;

/// <summary>
/// Represents a step in a route.
/// </summary>
/// <param name="RouteStepId">The unique identifier of the route step.</param>
/// <param name="StartNodeId">The identifier of the start node.</param>
/// <param name="EdgeId">The identifier of the edge. Can be null if the step does not involve an edge.</param>
/// <param name="EndNodeId">The identifier of the end node. Can be equal to the start node.</param>
/// <param name="ActionAtEndNode">The action to be performed at the end node. Can be null if no action is required.</param>
[PublicAPI]
public record RouteStep(Guid RouteStepId, Guid StartNodeId, Guid? EdgeId, Guid EndNodeId, AgvAction? ActionAtEndNode);