using Gebhardt.Acc.Common.Domain.Models.Graphs;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.ReadModels;

/// <summary>
/// The current route of the Agv. It is null when the Agv has completed the last action.
/// </summary>
/// <param name="CorrelationId">The CorrelationId of the Agv.</param>
/// <param name="RemainingNodes">The remaining nodes.</param>
/// <param name="RemainingEdges">The remaining edges.</param>
public record CurrentAgvRouteReadModel([UsedImplicitly] Guid CorrelationId, GraphNode[] RemainingNodes, Guid[] RemainingEdges);