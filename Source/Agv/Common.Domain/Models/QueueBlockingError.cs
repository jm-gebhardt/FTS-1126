namespace Gebhardt.Acc.Common.Domain.Models;

/// <summary>
/// Represents an error occuring when an agv got manually pushed
/// out of the queue and still blocks queue resources.
/// </summary>
public record QueueBlockingError : AgvError
{
    /// <inheritdoc cref="QueueBlockingError"/>>
    public QueueBlockingError(Guid correlationId, DateTime received) : base(correlationId,
        "Agv blocking queue",
        "Agv manually removed from queue or went offline in queue. Solve queue resource blocking if agv is not in or heading towards queue anymore, otherwise only remove error.",
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