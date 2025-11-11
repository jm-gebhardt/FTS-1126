using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.TransportOrder.Domain.Constants;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;

/// <summary>
/// Provides a reason why an action group failed.
/// </summary>
public enum FailReason : byte
{
    /// <summary>
    /// Initial value.
    /// </summary>
    None = 0,

    /// <summary>
    /// A target station is locked.
    /// </summary>
    LockedStation = 1,

    /// <summary>
    /// A target station has the wrong type.
    /// </summary>
    WrongStation = 2
}

/// <summary>
/// Extension methods for <see cref="FailReason"/>.
/// </summary>
public static class FailReasonExtensions
{
    /// <summary>
    /// Converts the <see cref="FailReason"/> to a <see cref="Reason"/>.
    /// </summary>
    /// <returns></returns>
    public static Reason? ToReason(this FailReason failReason)
    {
        string? template = failReason switch
        {
            FailReason.None => Reasons.ActionFailedForUnknownReason,
            FailReason.WrongStation => Reasons.ActionFailedWrongTargetType,
            FailReason.LockedStation => Reasons.ActionFailedLocked,
            _ => null
        };
        return template != null ? new Reason(template) : null;
    }
}