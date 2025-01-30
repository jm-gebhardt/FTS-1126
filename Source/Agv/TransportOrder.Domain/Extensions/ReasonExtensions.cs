using Gebhardt.Acc.Common.Domain.Models;

namespace Gebhardt.Acc.TransportOrder.Domain.Extensions;

/// <summary>
/// Extension methods for Reason.
/// </summary>
public static class ReasonExtensions
{
    /// <summary>
    /// Flag if pause reason is lock station reason.
    /// </summary>
    /// <param name="reason"></param>
    public static bool IsPauseReasonLockedStation(this Reason reason)
    {
        return reason.Template == Constants.Reasons.LockedStation;
    }
}