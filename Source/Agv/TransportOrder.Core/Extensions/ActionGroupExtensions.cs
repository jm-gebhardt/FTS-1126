using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Extensions;

/// <summary>
/// Extension methods for <see cref="ActionGroup"/>
/// </summary>
public static class ActionGroupExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="actionGroup"></param>
    public static void UpdateActionGroupState(this ActionGroup actionGroup)
    {
        if (actionGroup.Actions.All(a => a.State == ActionState.Completed))
        {
            actionGroup.State = ActionGroupState.Completed;
        }
        else if (actionGroup.Actions.All(a => a.State == ActionState.Created))
        {
            actionGroup.State = ActionGroupState.Created;
        }
        else if (actionGroup.Actions.Any(a => a.State == ActionState.Started))
        {
            actionGroup.State = ActionGroupState.Started;
        }
    }

    /// <summary>
    /// Sets the AssignedAgv of ActionGroup
    /// </summary>
    /// <param name="actionGroup"></param>
    /// <param name="agvToAssign"></param>
    public static ActionGroup AssignAgvToActionGroup(this ActionGroup actionGroup, IAgv agvToAssign)
    {
        if (actionGroup.AssignedAgv == null)
        {
            actionGroup.AssignedAgv = agvToAssign.CorrelationId;
        }
        else if (actionGroup.AssignedAgv != agvToAssign.CorrelationId)
        {
            throw new NotInCorrectStateException($"Trying to assign agv ({agvToAssign.CorrelationId}) to Action group with already existing assigned agv ({actionGroup.AssignedAgv}).");
        }
        return actionGroup;
    }
}