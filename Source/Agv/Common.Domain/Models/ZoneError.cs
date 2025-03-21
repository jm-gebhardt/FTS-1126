namespace Gebhardt.Acc.Common.Domain.Models;

/// <summary>
/// Represents a zone error.
/// </summary>
public record ZoneError : AgvError
{
    /// <inheritdoc cref="ZoneError"/>>
    public ZoneError(Guid correlationId,
        DateTime received, string zoneId) : base(correlationId,
        "Agv offline in zone",
        $"Agv went offline in zone {zoneId}",
        "FTS-4141",
        ErrorLevel.Warning,
        "Zone error",
        ErrorCategory.General,
        null,
        received,
        ErrorFlag.Agv)
    {
    }
}