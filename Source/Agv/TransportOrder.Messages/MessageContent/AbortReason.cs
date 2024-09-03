using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.TransportOrder.Domain.Constants;

namespace Gebhardt.Acc.TransportOrder.Messages.MessageContent;

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
    External = 2
}

/// <summary>
/// Extension methods for <see cref="AbortReason"/>.
/// </summary>
public static class AbortReasonExtensions
{
    /// <summary>
    /// Converts the <see cref="AbortReason"/> to a <see cref="Reason"/>.
    /// </summary>
    /// <param name="abortReason"></param>
    /// <returns></returns>
    public static Reason? ToReason(this AbortReason abortReason)
    {
        string? template = abortReason switch
        {
            AbortReason.ReactivatedFromCharging => Reasons.ActionGroupAbortedToReactivateFromCharging,
            AbortReason.WebApi => Reasons.ActionGroupAbortedByWebApi,
            AbortReason.External => Reasons.ActionGroupAbortedByExternal,
            _ => null
        };
        return template != null ? new Reason(template) : null;
    }
}