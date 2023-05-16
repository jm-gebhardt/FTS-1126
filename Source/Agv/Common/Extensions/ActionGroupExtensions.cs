using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.Common.Extensions;

/// <summary>
/// Extension methods for <see cref="ActionGroup"/>
/// </summary>
public static class ActionGroupExtensions
{
    /// <summary>
    /// Updates the state of the ActionGroup based on the state of its Actions
    /// </summary>
    /// <param name="actionGroup"></param>
    public static void UpdateActionGroupState(this ActionGroup actionGroup)
    {
        ICollection<AgvAction> actions = actionGroup.GetRequiredActions();
        if (actions.All(a => a.State == ActionState.Completed))
        {
            actionGroup.State = ActionGroupState.Completed;
        }
        else if (actions.All(a => a.State == ActionState.Created))
        {
            actionGroup.State = ActionGroupState.Created;
        }
        else if (actions.Any(a => a.State == ActionState.Started))
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
            throw new ActionGroupAlreadyAssignedException(actionGroup.AssignedAgv, agvToAssign.CorrelationId);
        }
        return actionGroup;
    }

    /// <summary>
    /// Makes sure that the ActionGroup has Actions and returns it
    /// </summary>
    /// <param name="actionGroup"></param>
    /// <returns>The Actions of the ActionGroup</returns>
    /// <exception cref="ArgumentException"></exception>
    public static ICollection<AgvAction> GetRequiredActions(this ActionGroup actionGroup)
    {
        actionGroup.RequireActions();
        return actionGroup.Actions!;
    }

    /// <summary>
    /// Makes sure that the ActionGroup has Actions
    /// </summary>
    /// <param name="actionGroup"></param>
    /// <exception cref="ArgumentException"></exception>
    // ReSharper disable once MemberCanBePrivate.Global
    public static void RequireActions(this ActionGroup actionGroup)
    {
        ICollection<AgvAction>? actionGroupActions = actionGroup.Actions;
        if (actionGroupActions == null)
        {
            throw new ArgumentException("ActionGroup must have Actions");
        }
    }
}