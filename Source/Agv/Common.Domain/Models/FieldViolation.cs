namespace Gebhardt.Acc.Common.Domain.Models;

/// <summary>
/// Represents a field violation error.
/// </summary>
public record FieldViolation : AgvError
{
    /// <inheritdoc cref="FieldViolation"/>>
    public FieldViolation(Guid correlationId,
        DateTime received) : base(correlationId,
        "Field violation",
        "Safety state warning: field violation",
        "FVE-4242",
        ErrorLevel.Warning,
        "Field violation",
        ErrorCategory.General,
        null,
        received,
        ErrorFlag.Agv,
        AgvErrorSource.Agv
    )
    {
    }
}