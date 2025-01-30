using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.TransportOrder.Domain.Constants;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;

/// <summary>
/// Provides a reason why an action group was paused.
/// </summary>
public enum PauseReason : byte
{
    /// <summary>
    /// Initial value.
    /// </summary>
    None = 0,

    /// <summary>
    /// Triggered during station lock.
    /// </summary>
    LockedStation = 1,

    /// <summary>
    /// Triggered during change to user mode.
    /// </summary>
    UserMode = 2,

    /// <summary>
    /// Triggered during change to lock mode.
    /// </summary>
    LockMode = 3,

    /// <summary>
    /// Triggered during change to maintenance mode.
    /// </summary>
    MaintenanceMode = 4
}

/// <summary>
/// Extension methods for <see cref="PauseReason"/>.
/// </summary>
public static class PauseReasonExtensions
{
    /// <summary>
    /// Converts the <see cref="PauseReason"/> to a <see cref="Reason"/>.
    /// </summary>
    /// <returns></returns>
    public static Reason? ToReason(this PauseReason pauseReason)
    {
        string? template = pauseReason switch
        {
            PauseReason.LockedStation => Reasons.LockedStation,
            PauseReason.UserMode => Reasons.AgvInUserMode,
            PauseReason.LockMode => Reasons.AgvInLockMode,
            PauseReason.MaintenanceMode => Reasons.AgvInMaintenanceMode,
            _ => null
        };
        return template != null ? new Reason(template) : null;
    }
}