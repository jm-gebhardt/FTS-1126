namespace Gebhardt.StoreWare.Agv.Common.Messages.Actions;

/// <summary>
/// States of ActionGroup
/// </summary>
public enum ActionGroupState
{
    /// <summary>
    /// Action is created
    /// </summary>
    Created,

    /// <summary>
    /// Action has been started
    /// </summary>
    Started,

    /// <summary>
    /// Action has been completed
    /// </summary>
    Completed
}