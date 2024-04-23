using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.TransportOrder.Domain.Constants;

namespace Gebhardt.Acc.TransportOrder.Messages.MessageContent;

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

/// <summary>
/// Extension methods for <see cref="CancelReason"/>.
/// </summary>
public static class CancelReasonExtensions
{
    /// <summary>
    /// Converts the <see cref="CancelReason"/> to a <see cref="Reason"/>.
    /// </summary>
    /// <param name="cancelReason"></param>
    /// <returns></returns>
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