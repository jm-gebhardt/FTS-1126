using Gebhardt.StoreWare.Agv.Common.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Constants;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent;

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
    ReactivatedFromCharging = 1
}

public static class AbortReasonExtensions
{
    public static Reason? ToReason(this AbortReason abortReason)
    {
        string? template = abortReason switch
        {
            AbortReason.ReactivatedFromCharging => Reasons.ActionGroupAbortedToReactivateFromCharging,
            AbortReason.WebApi => Reasons.ActionGroupAbortedByWebApi,
            _ => null
        };
        return template != null ? new Reason(template) : null;
    }
}