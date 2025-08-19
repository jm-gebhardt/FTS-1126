namespace Gebhardt.Acc.Common.Domain.Models;

/// <summary>
/// Represents a cargo mismatch error, i.e.
/// an error indicating the load on an agv
/// does not match its current or last order state.
/// </summary>
public record CargoMismatchError : AgvError
{
    /// <inheritdoc cref="CargoMismatchError"/>>
    public CargoMismatchError(Guid correlationId,
        DateTime received, bool hasCargo) : base(correlationId,
        "Agv cargo state mismatch: check load",
        $"Cargo state {hasCargo} does not match order state. Check load",
        "FTS-4949",
        ErrorLevel.Warning,
        "Cargo mismatch error",
        ErrorCategory.General,
        null,
        received,
        ErrorFlag.Agv,
        AgvErrorSource.System
    )
    {
    }
}