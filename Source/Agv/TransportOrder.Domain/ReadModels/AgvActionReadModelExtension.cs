using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace TransportOrder.Domain.ReadModels;

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
            action.ActionType, action.TargetNodeId, AgvGraph.NodeType.Charge, action.Nodes, 
            action.CreatedDate, action.StartedDate, action.CompletedDate);
    }
}