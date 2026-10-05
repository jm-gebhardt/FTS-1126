using Gebhardt.Acc.Common.Domain.Models.Graphs;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.ReadModels;

/// <summary>
/// The current route of the Agv. It is null when the Agv has completed the last action.
/// </summary>
/// <param name="CorrelationId">The CorrelationId of the Agv.</param>
/// <param name="RemainingNodes">The remaining nodes.</param>
/// <param name="RemainingEdges">The remaining edges.</param>
/// <param name="BaseNodeCount">The number of leading remaining nodes that belong to the base segment.</param>
public record CurrentAgvRouteReadModel(
    [UsedImplicitly] Guid CorrelationId,
    GraphNode[] RemainingNodes,
    Guid[] RemainingEdges,
    int BaseNodeCount = 0)
{
    /// <summary>Annotates this route with how many leading remaining nodes belong to the base segment.</summary>
    public CurrentAgvRouteReadModel AnnotateBase(IEnumerable<Guid>? baseNodeIds)
    {
        if (baseNodeIds == null)
        {
            return this;
        }

        var baseIds = baseNodeIds.ToHashSet();
        int baseNodeCount = RemainingNodes.TakeWhile(n => baseIds.Contains(n.Id)).Count();
        return this with { BaseNodeCount = baseNodeCount };
    }
}