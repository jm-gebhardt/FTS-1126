namespace Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;

/// <summary>
/// States of ActionGroup
/// </summary>
public enum ActionGroupState
{
    /// <summary>
    /// Action group is created
    /// </summary>
    Created,

    /// <summary>
    /// Action group has been started
    /// </summary>
    Started,

    /// <summary>
    /// Action group has been completed
    /// </summary>
    Completed,

    /// <summary>
    /// Action group has been canceled
    /// </summary>
    Canceled,

    /// <summary>
    /// Action group failed
    /// </summary>
    Failed,

    /// <summary>
    /// Action group is invalid
    /// </summary>
    Invalid
}