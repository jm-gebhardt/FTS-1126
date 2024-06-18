using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Messages.ActionGroups;

/// <summary>
/// State of an AGV action
/// </summary>
public enum ActionState
{
    /// <summary>
    /// Action created
    /// </summary>
    Created,

    /// <summary>
    /// Action transmitted to DeviceCommunication
    /// </summary>
    [PublicAPI]
    Started,

    /// <summary>
    /// Action completed
    /// </summary>
    [PublicAPI]
    Completed,

    /// <summary>
    /// Action has been canceled
    /// </summary>
    Canceled,

    /// <summary>
    /// Action failed
    /// </summary>
    Failed,

    /// <summary>
    /// Action is invalid.
    /// </summary>
    Invalid,

    /// <summary>
    /// Action aborted.
    /// </summary>
    Aborted,

    /// <summary>
    /// Action is Paused
    /// </summary>
    Paused
}