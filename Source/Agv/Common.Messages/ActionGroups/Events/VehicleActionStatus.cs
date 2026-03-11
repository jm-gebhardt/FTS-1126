using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Messages.ActionGroups.Events;

/// <summary>
/// The status of the action the agv runs on a node/edge or as an instant action.
/// </summary>
[PublicAPI]
public enum VehicleActionStatus
{
    /// <summary>Waiting for the trigger (passing the node, entering the edge).</summary>
    Waiting,

    /// <summary>Action is being initialized.</summary>
    Initializing,

    /// <summary>Action is running.</summary>
    Running,

    /// <summary>Paused by instantAction or external trigger.</summary>
    Paused,

    /// <summary>Action completed successfully.</summary>
    Finished,

    /// <summary>Action could not be performed.</summary>
    Failed
}