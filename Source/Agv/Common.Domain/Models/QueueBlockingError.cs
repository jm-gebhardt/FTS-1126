namespace Gebhardt.Acc.Common.Domain.Models;

/// <summary>
/// Represents an error occuring when an agv got manually pushed
/// out of the queue and still blocks queue resources.
/// </summary>
public record QueueBlockingError : AgvError
{
    /// <inheritdoc cref="QueueBlockingError"/>>
    public QueueBlockingError(Guid correlationId, DateTime received) : base(correlationId,
        "Agv blockiert eine Warteschlange",
        "Das Fahrzeug wurde manuell aus der Warteschlange weg bewegt oder ist in der Warteschlange offline gegangen. " +
        "Wenn das Fahrzeug nicht mehr in der Warteschlange steht und sich auch gerade nicht dort hin bewegt, setzen Sie die Belegungen zurück. Andernfalls setzen Sie nur die Warnung zurück.",
        "FTS-5353",
        ErrorLevel.Warning,
        "Queue blocking error",
        ErrorCategory.General,
        null,
        received,
        ErrorFlag.Agv,
        AgvErrorSource.System
    )
    {
    }
}