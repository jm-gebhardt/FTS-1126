using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Profiles;
using JetBrains.Annotations;
using ActionGroup = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.ActionGroup;
using AgvAction = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.AgvAction;

namespace Gebhardt.Acc.TransportOrder.Test.Helper;

public static class AgvActionGenerator
{
    public static ActionGroup GenerateChargeActionGroup(string targetNodeId = "charger", Guid? assignedAgv = null,
        ActionState actionStateMove = ActionState.Created, ActionState actionStateCharge = ActionState.Created, DateTime? createdDate=null, DateTime? completedDate = null)
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
        var actionGroup = new ActionGroup(groupId,
            string.Empty,
            new List<AgvAction>
            {
                moveAction,
                chargeAction
            },
            ActionGroupCreator.TransportOrder)
        {
            AssignedAgv = assignedAgv,
            CreatedDate = createdDate ?? DateTime.UtcNow
        };
        actionGroup.UpdateActionGroupState();
        actionGroup.Queue();
        if (completedDate != null)
        {
            actionGroup.GetType().GetProperty(nameof(ActionGroup.CompletedDate))!.SetValue(actionGroup, completedDate);
        }

        return actionGroup;
    }

    private static AgvAction GenerateChargeAction(Guid? groupId = null, string targetNodeId = "charger", int sequence = 0) => GenerateAction(groupId, targetNodeId, ActionType.Charge, sequence);

    public static AgvAction GenerateAction(Guid? groupId = null, string targetNodeId = "targetNode", ActionType actionType = ActionType.Move, int sequence = 0) =>
        new(Guid.NewGuid(), actionType, targetNodeId, groupId ?? Guid.NewGuid())
        {
            Sequence = sequence
        };

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

    public static ActionGroup GenerateParkActionGroup(
        string? targetNodeId = null,
        ActionState actionState = ActionState.Created,
        Guid? assignedAgv = null,
        DateTime? completedDate = null
    )
    {
        var groupId = Guid.NewGuid();
        var parkAction = new AgvAction(Guid.NewGuid(), ActionType.Move, targetNodeId ?? AgvGraphGenerator.ParkClose.Id, groupId)
        {
            Sequence = 0
        };
        if (targetNodeId != null)
        {
            parkAction.Nodes = [targetNodeId];
        }
        parkAction.UpdateActionState(actionState);
        var actionGroup = new ActionGroup(groupId, string.Empty, [parkAction], ActionGroupCreator.TransportOrder) { AssignedAgv = assignedAgv };
        actionGroup.UpdateActionGroupState();
        if (completedDate != null)
        {
            actionGroup.GetType().GetProperty(nameof(ActionGroup.CompletedDate))!.SetValue(actionGroup, completedDate);
        }
        return actionGroup;
    }

    public static ActionGroup GeneratePickDropActionGroupWithActionDuration(string? source = null, string? destination = null, double actionDuration = 0)
    {
        var actionGroup = GeneratePickDropActionGroup(source: source, destination: destination);
        foreach (var action in actionGroup.Actions)
        {
            actionGroup.StartAction(action.CorrelationId, [], []);
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
        bool hasPriority = false,
        bool isQueued = true,
        Dictionary<InfoToAgvKey, string>? infoToAgv=null,
        Dictionary<InfoToAgvKey, string>? dropInfoToAgv = null
    )
    {
        const string orderId = "orderId";
        var groupId = Guid.NewGuid();

        var pickActionGuid = Guid.NewGuid();
        var dropActionGuid = Guid.NewGuid();
        var pickAction = new AgvAction(pickActionGuid, ActionType.Pick, source ?? AgvGraphGenerator.SourceClose.Id, groupId)
        {
            Sequence = partial == 3 ? 1 : 0,
            InfoToAgv = infoToAgv
        };
        pickAction.UpdateActionState(pickActionState);
        var dropAction = new AgvAction(dropActionGuid, ActionType.Drop, destination ?? AgvGraphGenerator.DestinationClose.Id, groupId)
        {
            Sequence = partial == 3 ? 0 : 1,
            InfoToAgv = dropInfoToAgv
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
        if (isQueued)
        {
            actionGroup.Queue();
        }
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

    public static ActionGroup GenerateIncompletePickDropActionGroupWithoutPickTarget(
        ActionState pickActionState = ActionState.Created,
        ActionState dropActionState = ActionState.Created,
        ActionState moveActionState = ActionState.Created,
        string? moveDestination = null,
        string? destination = null,
        Guid? assignedAgv = null,
        DateTime createdDate = default,
        DateTime? completedDate = null,
        DateTime? startedDate = null,
        TimeSpan? duration = null,
        bool hasPriority = false,
        Dictionary<InfoToAgvKey, string>? infoToAgv = null
    )
    {
        const string orderId = "orderId";
        var groupId = Guid.NewGuid();

        var pickActionGuid = Guid.NewGuid();
        var dropActionGuid = Guid.NewGuid();
        var moveActionGuid = Guid.NewGuid();

        var moveAction = new AgvAction(moveActionGuid, ActionType.Move, moveDestination ?? AgvGraphGenerator.Waypoint3.Id, groupId)
        {
            Sequence = 0
        };
        moveAction.UpdateActionState(moveActionState);

        var pickAction = new AgvAction(pickActionGuid, ActionType.Pick, string.Empty, groupId)
        {
            Sequence = 1,
            InfoToAgv = infoToAgv
        };
        pickAction.UpdateActionState(pickActionState);
        var dropAction = new AgvAction(dropActionGuid, ActionType.Drop, destination ?? AgvGraphGenerator.DestinationClose.Id, groupId)
        {
            Sequence = 2
        };
        dropAction.UpdateActionState(dropActionState);

        var actions = new List<AgvAction> { moveAction, pickAction, dropAction };

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

    public static ActionGroup GenerateIncompletePickDropActionGroupWithoutDropTarget(
        ActionState pickActionState = ActionState.Created,
        ActionState dropActionState = ActionState.Created,
        ActionState moveActionState = ActionState.Created,
        string? moveDestination = null,
        string? source = null,
        Guid? assignedAgv = null,
        DateTime createdDate = default,
        DateTime? completedDate = null,
        DateTime? startedDate = null,
        TimeSpan? duration = null,
        bool hasPriority = false,
        Dictionary<InfoToAgvKey, string>? infoToAgv = null
    )
    {
        const string orderId = "orderId";
        var groupId = Guid.NewGuid();

        var pickActionGuid = Guid.NewGuid();
        var dropActionGuid = Guid.NewGuid();
        var moveActionGuid = Guid.NewGuid();

        var pickAction = new AgvAction(pickActionGuid, ActionType.Pick, source ?? AgvGraphGenerator.SourceClose.Id, groupId)
        {
            Sequence = 0,
            InfoToAgv = infoToAgv
        };
        pickAction.UpdateActionState(pickActionState);

        var moveAction = new AgvAction(moveActionGuid, ActionType.Move, moveDestination ?? AgvGraphGenerator.Waypoint3.Id, groupId)
        {
            Sequence = 1
        };
        moveAction.UpdateActionState(moveActionState);

        var dropAction = new AgvAction(dropActionGuid, ActionType.Drop, string.Empty, groupId)
        {
            Sequence = 2
        };
        dropAction.UpdateActionState(dropActionState);

        var actions = new List<AgvAction> { pickAction, moveAction, dropAction };

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

    public static ActionGroup GenerateMoveActionGroup(string? targetNodeId = null, bool isManual = false, Guid? assignedAgv = null, TimeSpan? duration = null, DateTime? createdDate = null)
    {
        const string orderId = "move";
        var groupId = Guid.NewGuid();
        var moveAction = new AgvAction(Guid.NewGuid(), ActionType.Move, targetNodeId ?? AgvGraphGenerator.Waypoint1.Id, groupId);
        if (targetNodeId != null)
        {
            moveAction.Nodes = [targetNodeId];
        }

        var actionGroup = new ActionGroup(groupId,
            orderId,
            new List<AgvAction>
            {
                moveAction
            },
            isManual: isManual,
            hasPriority: true,
            creator: ActionGroupCreator.TransportOrderWebApi,
            assignedAgv: assignedAgv)
        {
            EstimatedDuration = duration,
            CreatedDate = createdDate ?? DateTime.UtcNow
        };
        actionGroup.Queue();
        return actionGroup;
    }

    public static ActionGroup GenerateMoveToCoordinateActionGroup(AgvPosition? targetPosition = null, 
        bool isManual = false, 
        Guid? assignedAgv = null, 
        TimeSpan? duration = null,
        ActionState moveActionState = ActionState.Created,
        DateTime? completedDate = null)
    {
        const string orderId = "move";
        var groupId = Guid.NewGuid();
        var target = new ActionTarget(12, 12, 0);
        if (targetPosition != null)
        {
            target = new ActionTarget(targetPosition.X, targetPosition.Y, targetPosition.Theta);
        }
        var moveAction = new AgvAction(correlationId: Guid.NewGuid(), actionType: ActionType.Move, target:target , parentId: groupId)
        {
            Nodes = []
        };
        moveAction.UpdateActionState(moveActionState);
        var actionGroup = new ActionGroup(groupId, orderId, new List<AgvAction> { moveAction }, isManual: isManual, hasPriority: true, creator: ActionGroupCreator.TransportOrderWebApi, assignedAgv: assignedAgv) { EstimatedDuration = duration };
        actionGroup.UpdateActionGroupState();

        if (completedDate != null)
        {
            moveAction.GetType().GetProperty(nameof(AgvAction.CompletedDate))!.SetValue(moveAction, completedDate);
        }
        if (completedDate != null)
        {
            actionGroup.GetType().GetProperty(nameof(ActionGroup.CompletedDate))!.SetValue(actionGroup, completedDate);
        }
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
                actionGroup.StartAction(actionId, [], []);
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

            case ActionGroupState.Paused:
                actionGroup.State = ActionGroupState.Paused;
                break;
        }
        return actionGroup;
    }

    public static Common.Messages.ActionGroups.ActionGroup ConvertToMessage(ActionGroup actionGroup)
    {
        var mapper = MappingHelper.GetMapper(new ActionProfile());
        return mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup);
    }

    public static ActionGroup ConvertFromMessage(Common.Messages.ActionGroups.ActionGroup actionGroup)
    {
        var mapper = MappingHelper.GetMapper(new ActionProfile());
        return mapper.Map<ActionGroup>(actionGroup);
    }
}