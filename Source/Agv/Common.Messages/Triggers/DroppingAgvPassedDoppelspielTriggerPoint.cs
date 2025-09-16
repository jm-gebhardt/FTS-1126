
using Gebhardt.Acc.Common.Domain.Models.Graphs;

namespace Gebhardt.Acc.Common.Messages.Triggers;

/// <summary>
/// Represents the event raised when a dropping agv passed the Doppelspiel trigger point
/// </summary>
/// <param name="DropActionTarget">The target of the action groups drop action.</param>
/// <param name="AgvCorrelationId">The correlation id of the agv.</param>
/// <param name="DroppingHostOrderCorrelationId">The correlation id of the host order of the drop.</param>
/// <param name="NextPickHostOrderCorrelationId">The correlation id of the host order of the next pick.</param>
/// <param name="ShouldPickNext">If true the agv is going to pick next.</param>
/// <param name="PickNode">The pick node.</param>
public record DroppingAgvPassedDoppelspielTriggerPoint(GraphNode DropActionTarget, Guid AgvCorrelationId, Guid DroppingHostOrderCorrelationId, Guid? NextPickHostOrderCorrelationId, bool ShouldPickNext, GraphNode? PickNode);