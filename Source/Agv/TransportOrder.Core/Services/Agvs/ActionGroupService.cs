using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Registries;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;

/// <inheritdoc />
public class ActionGroupService : IActionGroupService
{
    private readonly IActionGroupRegistry _actionGroupRegistry;

    /// <summary>
    /// ctor for AgvActionGroupService
    /// </summary>
    /// <param name="actionGroupRegistry"></param>
    public ActionGroupService(IActionGroupRegistry actionGroupRegistry)
    {
        _actionGroupRegistry = actionGroupRegistry;
    }

    /// <inheritdoc />
    public ActionGroup GetActionGroup(Guid actionGroupCorrelationId)
    {
        return _actionGroupRegistry.GetActionGroupByParentId(actionGroupCorrelationId);
    }

    /// <inheritdoc />
    public async Task UpdateActionAndGroupStateAsync(AgvAction action, IAgv agv, ActionState state)
    {
        var actionGroup = GetActionGroup(action.ParentId);

        var actionToUpdate = actionGroup.Actions.Single(a => a.CorrelationId == action.CorrelationId);
        actionToUpdate.State = state;

        if (actionGroup.AssignedAgv == null)
        {
            actionGroup.AssignedAgv = agv.CorrelationId;
        }
        else if(actionGroup.AssignedAgv != agv.CorrelationId)
        {
            throw new NotInCorrectStateException($"Trying to assign agv ({agv.CorrelationId}) to Action group with already existing assigned agv ({actionGroup.AssignedAgv}).");
        }
        
        UpdateActionGroupState(actionGroup);

        await _actionGroupRegistry.UpdateActionGroupAsync(actionGroup);
    }
    
    private void UpdateActionGroupState(ActionGroup actionGroup)
    {
        // TODO: replace action in Group
        if (actionGroup.Actions.All(a => a.State == ActionState.Completed))
        {
            actionGroup.State = ActionGroupState.Completed;
        }
        else if (actionGroup.Actions.All(a => a.State == ActionState.Created))
        {
            actionGroup.State = ActionGroupState.Created;
        }
        else if (actionGroup.Actions.Any(a => a.State == ActionState.Assigned))
        {
            actionGroup.State = ActionGroupState.Assigned;
        }
        else if (actionGroup.Actions.Any(a => a.State == ActionState.Started))
        {
            actionGroup.State = ActionGroupState.Started;
        }
    }
}