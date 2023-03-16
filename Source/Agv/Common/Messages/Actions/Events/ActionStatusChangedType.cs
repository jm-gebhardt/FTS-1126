namespace Gebhardt.StoreWare.Agv.Common.Messages.Actions.Events;

/// <summary>
/// When the status of the action changes
/// </summary>
public enum ActionStatusChangedType
{
    /// <summary>
    /// Action started
    /// </summary>
    Started,

    /// <summary>
    /// Action completed
    /// </summary>
    Completed,

    /// <summary>
    /// Action cancelled
    /// </summary>
    Cancelled,

    /// <summary>
    /// Action failed to cancel
    /// </summary>
    CancelFailed,

    /// <summary>
    /// Action failed
    /// </summary>
    Failed
}