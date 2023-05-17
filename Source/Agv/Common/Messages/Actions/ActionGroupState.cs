using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.Common.Messages.Actions;

/// <summary>
/// State of an AGV action group
/// </summary>
public enum ActionGroupState
{
    /// <summary>
    /// Action created
    /// </summary>
    Created,

    /// <summary>
    /// Action transmitted to DeviceCommunication
    /// </summary>
    Started,

    /// <summary>
    /// Action completed
    /// </summary>
    Completed
}