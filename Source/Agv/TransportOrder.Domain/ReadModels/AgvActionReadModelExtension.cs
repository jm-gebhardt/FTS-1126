using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups;

namespace Gebhardt.Acc.TransportOrder.Domain.ReadModels;

/// <summary>
/// Provides extension methods for testing the action read model.
/// </summary>
public static class AgvActionReadModelExtension
{
    /// <summary>
    /// Converts an action to its read model with node types using made-up fake node types.
    /// </summary>
    /// <param name="action">The action to be converted.</param>
    public static AgvActionReadModel? ConvertFromAgvActionWithFakeNodeType(this AgvAction? action)
    {
        if (action == null)
        {
            return null;
        }

        return new AgvActionReadModel(action.CorrelationId, action.ParentId, action.Sequence, action.State,
            action.ActionType, action.Target.NodeId!, AgvGraph.NodeType.Charge, action.Nodes, action.Edges,
            action.CreatedDate, action.StartedDate, action.CompletedDate, action.PausedDate, action.PausedDuration);
    }
}