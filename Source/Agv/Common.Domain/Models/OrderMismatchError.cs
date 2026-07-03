namespace Gebhardt.Acc.Common.Domain.Models;

/// <summary>
/// Represents an order mismatch warning, i.e. the agv reports no order (idle)
/// although the fleet manager still has a started, uncompleted order assigned to it.
/// </summary>
public record OrderMismatchError : AgvError
{
    /// <inheritdoc cref="OrderMismatchError"/>>
    public OrderMismatchError(Guid correlationId, DateTime received) : base(correlationId,
        "Check Order",
        "AGV with an assigned order is shown as idle. Check the order and resend or abort it. " +
        "If you resend the order, the assigned order will be restarted to be completed.",
        "FTS-4670",
        ErrorLevel.Warning,
        "Order mismatch error",
        ErrorCategory.General,
        null,
        received,
        ErrorFlag.Agv,
        AgvErrorSource.System
    )
    {
    }
}
