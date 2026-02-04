using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.TransportOrder.Domain.Constants;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;

/// <summary>
/// Provides a reason why an action group was aborted.
/// </summary>
public enum AbortReason : byte
{
    /// <summary>
    /// Triggered by Web Api.
    /// </summary>
    WebApi = 0,

    /// <summary>
    /// The Agv was reactivated from charging.
    /// </summary>
    ReactivatedFromCharging = 1,

    /// <summary>
    /// Triggered by external service.
    /// </summary>
    External = 2,

    /// <summary>
    /// Triggered during station lock.
    /// </summary>
    LockedStation = 3,

    /// <summary>
    /// The Agv was reactivated from parking.
    /// </summary>
    ReactivatedFromParking = 4,

    /// <summary>
    /// A test plan has changed (added/updated/deleted).
    /// </summary>
    TestPlanChange = 5
}

/// <summary>
/// Extension methods for <see cref="AbortReason"/>.
/// </summary>
public static class AbortReasonExtensions
{
    /// <summary>
    /// Converts the <see cref="AbortReason"/> to a <see cref="Reason"/>.
    /// </summary>
    /// <returns></returns>
    public static Reason? ToReason(this AbortReason abortReason)
    {
        string? template = abortReason switch
        {
            AbortReason.ReactivatedFromCharging => Reasons.ActionGroupAbortedToReactivateFromCharging,
            AbortReason.ReactivatedFromParking => Reasons.ActionGroupAbortedToReactivateFromParking,
            AbortReason.WebApi => Reasons.ActionGroupAbortedByWebApi,
            AbortReason.External => Reasons.ActionGroupAbortedByExternal,
            AbortReason.LockedStation => Reasons.LockedStation,
            AbortReason.TestPlanChange => Reasons.ActionGroupAbortedByTestPlanChange,
            _ => null
        };
        return template != null ? new Reason(template) : null;
    }
}