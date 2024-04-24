using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Testing.Helpers;
using JetBrains.Annotations;
using ActionGroup = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.ActionGroup;
using AgvAction = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.AgvAction;

namespace Gebhardt.Acc.TransportOrder.Test.Helper;

public static class AgvActionGenerator
{
    public static ActionGroup GenerateChargeActionGroup(string targetNodeId = "charger", Guid? assignedAgv = null,
        ActionState actionStateMove = ActionState.Created, ActionState actionStateCharge = ActionState.Created)
    {
        var groupId = Guid.NewGuid();
        var moveAction = new AgvAction(Guid.NewGuid(), ActionType.Move, targetNodeId, groupId)
        {
            Sequence = 0
        };
        moveAction.UpdateActionState(actionStateMove);

        var chargeAction = GenerateChargeAction(groupId, targetNodeId, 1);
        chargeAction.UpdateActionState(actionStateCharge);

        chargeAction.Nodes = [targetNodeId];
        var actionGroup = new ActionGroup(groupId, string.Empty, new List<AgvAction> { moveAction, chargeAction }, ActionGroupCreator.TransportOrder) { AssignedAgv = assignedAgv };
        actionGroup.UpdateActionGroupState();

        return actionGroup;
    }

    public static AgvAction GenerateChargeAction(Guid? groupId = null, string targetNodeId = "charger", int sequence = 0) => GenerateAction(groupId, targetNodeId, ActionType.Charge, sequence);

    public static AgvAction GenerateAction(Guid? groupId = null, string targetNodeId = "targetNode", ActionType actionType = ActionType.Move, int sequence = 0) =>
        new(Guid.NewGuid(), actionType, targetNodeId, groupId ?? Guid.NewGuid())
        {
            Sequence = sequence
        };

    [UsedImplicitly]
    public static AgvAction GenerateParkAction(string? targetNodeId = null, Guid? parentId = null, int sequence = 0, ActionState actionState = ActionState.Created)
    {
        var parkAction = new AgvAction(Guid.NewGuid(), ActionType.Move, targetNodeId ?? AgvGraphGenerator.ParkClose.Id, parentId ?? Guid.NewGuid())
        {
            Sequence = sequence
        };
        if (targetNodeId != null)
        {
            parkAction.Nodes = [targetNodeId];
        }
        parkAction.UpdateActionState(actionState);
        return parkAction;
    }

    public static ActionGroup GeneratePickDropActionGroupWithActionDuration(string? source = null, string? destination = null, double actionDuration = 0)
    {
        var actionGroup = GeneratePickDropActionGroup(source: source, destination: destination);
        foreach (var action in actionGroup.Actions)
        {
            actionGroup.StartAction(action.CorrelationId, []);
            action.CompletedDate = action.StartedDate!.Value.AddSeconds(actionDuration);
            action.State = ActionState.Completed;
        }
        actionGroup.State = ActionGroupState.Completed;
        return actionGroup;
    }

    public static ActionGroup GeneratePickDropActionGroup(
        ActionState pickActionState = ActionState.Created,
        ActionState dropActionState = ActionState.Created,
        string? source = null,
        string? destination = null,
        Guid? assignedAgv = null,
        int partial = 0,
        DateTime createdDate = default,
        DateTime? completedDate = null,
        DateTime? startedDate = null,
        TimeSpan? duration = null,
        bool hasPriority = false
    )
    {
        const string orderId = "orderId";
        var groupId = Guid.NewGuid();

        var pickActionGuid = Guid.NewGuid();
        var dropActionGuid = Guid.NewGuid();
        var pickAction = new AgvAction(pickActionGuid, ActionType.Pick, source ?? AgvGraphGenerator.SourceClose.Id, groupId)
        {
            Sequence = partial == 3 ? 1 : 0
        };
        pickAction.UpdateActionState(pickActionState);
        var dropAction = new AgvAction(dropActionGuid, ActionType.Drop, destination ?? AgvGraphGenerator.DestinationClose.Id, groupId)
        {
            Sequence = partial == 3 ? 0 : 1
        };
        dropAction.UpdateActionState(dropActionState);

        List<AgvAction> actions = partial switch
        {
            1 => new List<AgvAction> { pickAction },
            2 => new List<AgvAction> { dropAction },
            3 => new List<AgvAction> { dropAction, pickAction },
            _ => new List<AgvAction> { pickAction, dropAction }
        };

        var actionGroup = new ActionGroup(groupId, orderId, actions, ActionGroupCreator.Wms)
        {
            AssignedAgv = assignedAgv,
            EstimatedDuration = duration,
            HasPriority = hasPriority
        };
        actionGroup.UpdateActionGroupState();
        if (createdDate != default)
        {
            actionGroup.GetType().GetProperty(nameof(ActionGroup.CreatedDate))!.SetValue(actionGroup, createdDate);
        }
        if (startedDate != null)
        {
            actionGroup.GetType().GetProperty(nameof(ActionGroup.StartedDate))!.SetValue(actionGroup, startedDate);
        }
        if (completedDate != null)
        {
            actionGroup.GetType().GetProperty(nameof(ActionGroup.CompletedDate))!.SetValue(actionGroup, completedDate);
        }
        return actionGroup;
    }

    public static ActionGroup GenerateMoveActionGroup(string? targetNodeId = null, bool isManual = false, Guid? assignedAgv = null, TimeSpan? duration = null)
    {
        const string orderId = "move";
        var groupId = Guid.NewGuid();
        var moveAction = new AgvAction(Guid.NewGuid(), ActionType.Move, targetNodeId ?? AgvGraphGenerator.Waypoint1.Id, groupId);
        if (targetNodeId != null)
        {
            moveAction.Nodes = [targetNodeId];
        }

        var actionGroup = new ActionGroup(groupId, orderId, new List<AgvAction> { moveAction }, isManual: isManual, hasPriority: true, creator: ActionGroupCreator.TransportOrderWebApi, assignedAgv: assignedAgv) { EstimatedDuration = duration };

        return actionGroup;
    }

    [UsedImplicitly]
    public static ActionGroup GenerateActionGroup(IList<AgvAction> actions, ActionGroupState actionGroupState = ActionGroupState.Created, string orderId = "", Guid? correlationId = null)
    {
        var groupId = correlationId ?? actions.Select(a => a.ParentId).FirstOrDefault(Guid.NewGuid());

        var actionGroup = new ActionGroup(groupId, orderId, actions, ActionGroupCreator.Wms);

        return SetActionGroupToState(actionGroup, actionGroupState);
    }

    public static ActionGroup SetActionGroupToState(ActionGroup actionGroup, ActionGroupState state)
    {
        if (!actionGroup.Actions.Any())
        {
            return actionGroup;
        }

        var actionId = actionGroup.Actions.First().CorrelationId;

        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        switch (state)
        {
            case ActionGroupState.Started:
                actionGroup.StartAction(actionId, []);
                break;

            case ActionGroupState.Completed:
                foreach (var action in actionGroup.Actions)
                {
                    actionGroup.CompleteAction(action.CorrelationId);
                }
                break;

            case ActionGroupState.Canceled:
                actionGroup.CancelActionGroup();
                break;

            case ActionGroupState.Failed:
                actionGroup.SetActionFailed(actionId);
                break;

            case ActionGroupState.Invalid:
                actionGroup.InvalidateAction(actionId);
                break;

            case ActionGroupState.Aborted:
                actionGroup.AbortAction(actionId, Enumerable.Empty<Reason>());
                break;
        }
        return actionGroup;
    }
}