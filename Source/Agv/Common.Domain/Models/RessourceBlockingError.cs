namespace Gebhardt.Acc.Common.Domain.Models;

/// <summary>
/// Represents a ressource blocking error
/// </summary>
public record RessourceBlockingError : AgvError
{
    /// <inheritdoc cref="RessourceBlockingError"/>>
    public RessourceBlockingError(Guid correlationId,
        DateTime received, string station) : base(correlationId,
        "Agv offline blocking ressource",
        $"Agv went offline with reservation for or blocking {station}",
        "FTS-4143",
        ErrorLevel.Warning,
        "Ressource error",
        ErrorCategory.General,
        null,
        received,
        ErrorFlag.Agv)
    {
    }
}