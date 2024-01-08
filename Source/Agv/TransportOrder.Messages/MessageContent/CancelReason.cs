using Gebhardt.StoreWare.Agv.Common.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Constants;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent;

/// <summary>
/// Provides a reason why an action group was canceled.
/// </summary>
public enum CancelReason : byte
{
    /// <summary>
    /// Triggered by Web Api.
    /// </summary>
    WebApi = 0
}

public static class CancelReasonExtensions
{
    public static Reason? ToReason(this CancelReason cancelReason)
    {
        string? template = cancelReason switch
        {
            CancelReason.WebApi => Reasons.ActionGroupCanceledByWebApi,
            _ => null
        };
        return template != null ? new Reason(template) : null;
    }
}