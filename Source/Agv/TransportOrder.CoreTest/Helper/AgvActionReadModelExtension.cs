using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.ReadModels;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;

public static class AgvActionReadModelExtension
{
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