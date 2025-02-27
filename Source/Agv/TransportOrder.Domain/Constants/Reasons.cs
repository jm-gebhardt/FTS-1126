namespace Gebhardt.Acc.TransportOrder.Domain.Constants;

/// <summary>
/// Result/Error codes for the transport order domain.
/// </summary>
public static class Reasons
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    public const string OrderIdIsNullOrEmpty = "OrderId is null or empty";
    public const string ActionStateMustBeCreated = "Action state must be created, but was {0}";
    public const string ActionGroupStateMustBeCreated = "Action group state must be created, but was {0}";
    public const string CorrelationIdIsNullOrEmpty = "CorrelationId is null or empty";
    public const string ParentIdOfEachActionMustMatchCorrelationIdOfActionGroup = "Parent id of each action must match correlation id of action group";
    public const string ActionSequenceIdsMustBeSet = "Action sequence Ids must be set.";
    public const string ActionSequenceIdsMustBeUnique = "Action sequence Ids must be unique";
    public const string ActionSequenceMustNotHaveGaps = "Action sequence must not have gaps";
    public const string ActionsWithinTheGroupMustBeOrderedBySequence = "Actions within the group must be ordered by sequence";

    public const string PickStationNodeDoesNotExist = "Source station with node \"{0}\" does not exist";
    public const string DropStationNodeDoesNotExist = "Target station with node \"{0}\" does not exist";
    public const string TargetNodeDoesNotExist = "Node \"{0}\" does not exist";
    public const string ActionsMustBeSet = "Actions must be set";
    public const string ManualActionMustHaveExactlyOneAction = "Manual action must have exactly one action";
    public const string LockedStation = "A target station is locked";
    public const string AgvInUserMode = "AGV in user mode";
    public const string AgvInLockMode = "AGV in lock mode";
    public const string AgvInMaintenanceMode = "AGV in maintenance mode";

    public const string ManualActionMustHaveAgvAssigned = "Manual action must have an Agv assigned";

    public const string AgvDoesNotExist = "Agv with correlation id \"{0}\" does not exist";

    public const string PickDropSequenceMustBeValid = "Pick/Drop sequence must be valid";

    public const string ActionGroupAbortedToReactivateFromParking = "ActionGroup aborted to reactivate Agv from parking";
    public const string ActionGroupAbortedToReactivateFromCharging = "ActionGroup aborted to reactivate Agv from charging";
    public const string ActionGroupAbortedByWebApi = "ActionGroup was aborted manually";
    public const string ActionGroupAbortedByExternal = "ActionGroup was aborted by an external system";

    public const string ActionGroupCanceledByWebApi = "ActionGroup was canceled manually";
    public const string ActionGroupCanceledByExternal = "ActionGroup was canceled by an external system";

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}