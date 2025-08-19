namespace Gebhardt.Acc.Common.Domain.Models;

/// <summary>
/// Represents a resource blocking error
/// </summary>
public record ResourceBlockingError : AgvError
{
    /// <inheritdoc cref="ResourceBlockingError"/>>
    public ResourceBlockingError(Guid correlationId,
        DateTime received, string station) : base(correlationId,
        "Agv offline blocking resource",
        $"Agv went offline with reservation for or blocking {station}",
        "FTS-4143",
        ErrorLevel.Warning,
        "Resource error",
        ErrorCategory.General,
        null,
        received,
        ErrorFlag.Agv,
        AgvErrorSource.System
    )
    {
    }
}